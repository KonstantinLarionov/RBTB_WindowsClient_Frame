using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BinanceMapper.Requests;
using BinanceMapper.Spot.Exchange.V3.Data;
using BinanceMapper.Spot.Exchange.V3.Requests;
using Microsoft.Win32;
using RBTB_WindowsClient.Integrations;
using RBTB_WindowsClient.Integrations.Binance;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace RBTB_WindowsClient_Frame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WsClientStrategyService _wsStrategyService;
        private BinanceRestClient _binanceRestClient;
        private TelegramClient _telegramRestClient;
        
        private decimal Volume;
        private bool IsTrading;
        private decimal TicksOut;
        private List<decimal> UseLevels = new List<decimal>();
        private Task MainTask = null;
        public MainWindow()
        {
            InitializeComponent();
            logger.Document.LineHeight = 2;
            
            LoadingOptions();
        }
        
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                MainTask = new Task(() => {
                    Dispatcher.BeginInvoke(delegate () {
                        Thread.Sleep(1000);
                        _telegramRestClient = new TelegramClient(chat: tb_tg.Text);
                        var ra = new RequestArranger(tb_api.Text, tb_secret.Text);
                        ra.ActualityWindow = 10000;
                        _binanceRestClient = new BinanceRestClient(ra);
                        _binanceRestClient.SetUrl("https://api.binance.com");
                        _wsStrategyService = new WsClientStrategyService();

                        Volume = Convert.ToDecimal(tb_btc.Text);
                        IsTrading = tgl_trade.IsChecked.Value;
                        TicksOut = Convert.ToDecimal(tb_pips.Text);

                        if (_binanceRestClient.RequestAccountInfo(out var data))
                        {
                            var balance = data.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();
                            var balanceBTC = data.Balances.Where(x => x.Asset == "BTC").FirstOrDefault();
                            Log("Баланс USDT: " + balance.Free.ToString());
                            Log("Баланс BTC: " + balanceBTC.Free.ToString());
                        }

                        Log($"Клиент успешно запущен\r\nВсе настройки применены\r\nОбъем входа: {Volume}\r\nТорговля: {IsTrading}\r\nПунктов на выход: {TicksOut}");
                        Log($"Подключение к серверу стратегий..");

                        _wsStrategyService.StrategyTradeEv += WsStrategyServiceOnStrategyTradeEv;
                        _wsStrategyService.ErrorEv += WsStrategyServiceOnErrorEv;
                        _wsStrategyService.OpenEv += WsStrategyServiceOnOpenEv;
                        _wsStrategyService.SetUrlServiceStrategy(tb_ss.Text);
                        _wsStrategyService.Start();
                    });
                });
                MainTask.Start();
            }
            catch {
                ButtonBase_OnClick(sender, e);
            }
        }
        
        private void MainWindow_OnClosing(object sender, CancelEventArgs e) => SavingOptions();
        private void WsStrategyServiceOnOpenEv(EventArgs e) => Log($"Успешно подключение.\r\nОжидаем ситуаций для торговли");
        private void WsStrategyServiceOnErrorEv(ErrorEventArgs e)
        {
            Log($"Ошибка подключения\r\n" + e.Message);
            _wsStrategyService.Stop();

            ButtonBase_OnClick(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Событие о том что нужно открывать сделки
        /// </summary>
        /// <param name="price"></param>
        /// <param name="symbol"></param>
        /// <param name="level"></param>
        /// <param name="datetime"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void WsStrategyServiceOnStrategyTradeEv(decimal price, string symbol, decimal level, DateTime datetime)
        {
            if(UseLevels.Contains(level))
                return;
            
            Log($"Найдена ситуация для торговли\r\nУровень: {level}, Цена: {price}, Инструмент: {symbol}");
            if (_binanceRestClient.RequestCurrentlyOpenedOrders(out var orders, symbol))
            {
                if (orders.Count != 0)
                {
                    Log($"Торговля запрещена. Есть открытые ордера на бирже.");
                    return;
                }
            }
            else
            {
                Log($"Ошибка связи с биржей. Невозможно запросить ордера.");
                return;
            }

            if (IsTrading)
            {
                Log("Торговля разрешена, открываю ордера..");
                if (_binanceRestClient.RequestAccountInfo(out var balances))
                {
                    var balance = balances.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();
                    var balanceBTC = balances.Balances.Where(x => x.Asset == "BTC").FirstOrDefault();
                    Log("Баланс USDT: " + balance.Free.ToString());
                    Log("Баланс BTC: " + balanceBTC.Free.ToString());
                }
                
                var order = new NewOrderRequest(symbol, OrderSide.Buy, OrderType.Market, Volume);
                if (_binanceRestClient.RequestNewOrder(out var data, out var error, "", order))
                {
                    Log($"Ордер покупки успешно выставлен\r\n{order.Price} - {order.Quantity}");
                }
                else
                {
                    Log($"Ошибка открытия ордера покупки\r\n{error.Message}");
                }
                
                var orderOut = new NewOrderRequest(symbol, OrderSide.Sell, OrderType.Limit, Volume);
                orderOut.Price = price + TicksOut;
                orderOut.TimeInForce = TimeInForce.GoodTillCancelled;
                
                if (_binanceRestClient.RequestNewOrder(out var resOut, out var er, null, orderOut))
                {
                    Log($"Ордер продажи успешно выставлен\r\n{resOut.Price} - {resOut.Quantity}");
                }
                else
                {
                    Log($"Ошибка открытия ордера продажи\r\n{error.Message}");
                }
            }
            else
            { Log("Автоматическая торговля запрещена.."); }
            UseLevels.Add(level);
            Thread.Sleep(5000);
        }

        private void Log(string text)
        {
            _telegramRestClient.SendMessage(text);
            Dispatcher.BeginInvoke((Action) delegate
            {
                logger.Document.Blocks.Add(new Paragraph(new Run($"[{DateTime.Now.ToShortDateString() +" "+ DateTime.Now.ToShortTimeString()}] " + text)));
            });
        }
        private void LoadingOptions()
        {
            
            using (StreamReader fs = new StreamReader(@"options.txt"))
            {
                int count = 0;
                while (true)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    switch (count)
                    {
                        case 0:
                            tb_api.Text = temp;
                            break;
                        case 1:
                            tb_secret.Text = temp;
                            break;
                        case 2:
                            tb_tg.Text = temp;
                            break;
                        case 3:
                            tb_btc.Text = temp;
                            break;
                        case 4:
                            tb_pips.Text = temp;
                            break;
                        case 5:
                            tgl_trade.IsChecked = Convert.ToBoolean(temp);
                            break;
                        case 6:
                            tb_ss.Text = temp;
                            break;
                        default:
                            break;
                    }

                    count++;
                }
            }
        }

        private void SavingOptions()
        {
            using (StreamWriter writer = new StreamWriter("options.txt", false))
            {
                writer.WriteLine(tb_api.Text);
                writer.WriteLine(tb_secret.Text);
                writer.WriteLine(tb_tg.Text);
                writer.WriteLine(tb_btc.Text);
                writer.WriteLine(tb_pips.Text);
                writer.WriteLine(tgl_trade.IsChecked);
                writer.WriteLine(tb_ss.Text);
            }
        }

        private string GetPrivateRsa(string path)
        {
            string temp = string.Empty;
            using (StreamReader fs = new StreamReader(path))
            {
                string line;
                while ((line = fs.ReadLine()) != null)
                {
                    temp += line + Environment.NewLine;
                }
            }

            return temp;
        }


        private void ButtonBase_OnClick2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            
            tb_secret.Text = ofd.FileName;
        }
    }
}
