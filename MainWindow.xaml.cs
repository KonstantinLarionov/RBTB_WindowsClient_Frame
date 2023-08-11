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
using RBTB_ServiceAccount.Client.Client;
using RBTB_WindowsClient_Frame.Controls;
using RBTB_WindowsClient_Frame.Integrations.MyNamespace;
using OrderType = BinanceMapper.Spot.Exchange.V3.Data.OrderType;
using TimeInForce = BinanceMapper.Spot.Exchange.V3.Data.TimeInForce;

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
		private TradesClient tradesRepo;
		private AccountClient _accountClient;

		private Guid userId = new Guid( "cf955f5a-c14c-4040-b439-38cf5736119f" );
        private decimal Volume;
        private bool IsTrading = true;
        private decimal TicksOut;
        private List<decimal> UseLevels = new List<decimal>();
        private Thread MainTask = null;
		private bool OrderHave = false;

		private WalletControl walletControl;
		private OptionsControl optionsControl;

		public MainWindow()
        {
            InitializeComponent();

			walletControl = new WalletControl( userId );
			optionsControl = new OptionsControl();

			main_space.Children.Add( optionsControl );
			_accountClient = new AccountClient( "http://188.186.238.120:5249", new System.Net.Http.HttpClient());
			_telegramRestClient = new TelegramClient( chat: optionsControl.tb_tg.Text );
			Log( "Включение робота загрузка настроек.." );
			optionsControl.logger.Document.LineHeight = 2;
            LoadingOptions();

			Log( "Настройки успешно загружены." );
			Log( "Робот готов к работе." );
		}

		private void MainTaskTrading()
		{
			Dispatcher.BeginInvoke( delegate ()
			{
				Thread.Sleep( 1000 );
				var ra = new RequestArranger( optionsControl.tb_api.Password, optionsControl.tb_secret.Password );
				ra.ActualityWindow = 10000;
				_binanceRestClient = new BinanceRestClient( ra );
				_binanceRestClient.SetUrl( "https://api.binance.com" );
				_wsStrategyService = new WsClientStrategyService();

				Volume = Convert.ToDecimal( optionsControl.tb_btc.Text );
				//IsTrading = tgl_trade.IsChecked.Value;
				TicksOut = Convert.ToDecimal( optionsControl.tb_pips.Text );

				SaveBalanceUSDT();

				Log( $"Клиент успешно запущен\r\nВсе настройки применены\r\nОбъем входа: {Volume}\r\nТорговля: {IsTrading}\r\nПунктов на выход: {TicksOut}" );
				Log( $"Подключение к серверу стратегий.." );

				_wsStrategyService.StrategyTradeEv += WsStrategyServiceOnStrategyTradeEv;
				_wsStrategyService.ErrorEv += WsStrategyServiceOnErrorEv;
				_wsStrategyService.OpenEv += WsStrategyServiceOnOpenEv;
				_wsStrategyService.SetUrlServiceStrategy( optionsControl.tb_ss.Text );
				_wsStrategyService.Start();
			} );
		}

		private void SaveBalanceUSDT()
		{
			if ( _binanceRestClient.RequestAccountInfo( out var data ) )
			{
				var balances = data.Balances
				.Where( x => x.Free != 0 )
				.ToList();
				var usdt_balance = 0.0m;
				foreach ( var item in balances )
				{
					if ( item.Asset == "USDT" )
					{
						usdt_balance += item.Locked + item.Free;
					}
					else if ( _binanceRestClient.RequestTickerInfo( item.Asset + "USDT", out var curr ) )
					{
						var price = curr.LastPrice;
						usdt_balance += item.Free * price;
						usdt_balance += item.Locked * price;
					}
				}

				_accountClient.Create4Async( new CreateWalletRequest() { UserId = userId, Symbol = "USDT", Balance = Convert.ToDouble(usdt_balance), Market = "Binance" } );

				Log( "Баланс USDT: " + usdt_balance.ToString() );
			}
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e) => SavingOptions();
        private void WsStrategyServiceOnOpenEv(EventArgs e) => Log($"Успешно подключение.\r\nОжидаем ситуаций для торговли");
        private void WsStrategyServiceOnErrorEv(ErrorEventArgs e)
        {
            Log($"Ошибка подключения\r\n" + e.Message);
            _wsStrategyService.Stop();
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
            
            //Log($"Найдена ситуация для торговли\r\nУровень: {level}, Цена: {price}, Инструмент: {symbol}");
            if (_binanceRestClient.RequestCurrentlyOpenedOrders(out var orders, symbol))
            {
                if (orders.Count != 0)
                {
                    //Log($"Торговля запрещена. Есть открытые ордера на бирже.");
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
				Log( "Сигнал торговли, открываю ордера.." );
				SaveBalanceUSDT();

				BuyMarketSellLimit( price, symbol, level );
			}
			else
            { Log("Автоматическая торговля запрещена.."); }
            UseLevels.Add(level);
            Thread.Sleep(5000);
        }

		private void BuyMarketSellLimit( decimal price, string symbol, decimal level )
		{
			var order = new NewOrderRequest( symbol, OrderSide.Buy, OrderType.Market, Volume );
			if ( _binanceRestClient.RequestNewOrder( out var data, out var error, "", order ) )
			{
				Log( $"Выставлена покупка {level}" );
			}
			else
			{
				Log( $"Ошибка открытия ордера покупки\r\n{error.Message}" );
			}

			var orderOut = new NewOrderRequest( symbol, OrderSide.Sell, OrderType.Limit, Volume );
			orderOut.Price = price + TicksOut;
			orderOut.TimeInForce = TimeInForce.GoodTillCancelled;

			if ( _binanceRestClient.RequestNewOrder( out var resOut, out var er, null, orderOut ) )
			{
				Log( $"Выставлена продажа {resOut.Price}" );
			}
			else
			{
				Log( $"Ошибка открытия ордера продажи\r\n{error.Message}" );
			}
		}

		private void Log(string text)
        {
            _telegramRestClient.SendMessage(text);
            Dispatcher.BeginInvoke((Action) delegate
            {
				var par = new Paragraph( new Run( $"[{DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()}] " + text, optionsControl.logger.Selection.Start ) );
				optionsControl.logger.Document.Blocks.InsertBefore( optionsControl.logger.Document.Blocks.FirstBlock, par);
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
							optionsControl.tb_api.Password = temp;
                            break;
                        case 1:
							optionsControl.tb_secret.Password = temp;
                            break;
                        case 2:
							optionsControl.tb_tg.Text = temp;
                            break;
                        case 3:
							optionsControl.tb_btc.Text = temp;
                            break;
                        case 4:
							optionsControl.tb_pips.Text = temp;
                            break;
                        case 5:
							optionsControl.tb_ss.Text = temp;
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
                writer.WriteLine( optionsControl.tb_api.Password );
                writer.WriteLine( optionsControl.tb_secret.Password );
                writer.WriteLine( optionsControl.tb_tg.Text);
                writer.WriteLine( optionsControl.tb_btc.Text);
                writer.WriteLine( optionsControl.tb_pips.Text);
                writer.WriteLine( optionsControl.tb_ss.Text);
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

			optionsControl.tb_secret.Password = ofd.FileName;
        }

		private void ToggleButton_Unchecked( object sender, RoutedEventArgs e )
		{
			MainTask.Abort();
		}

		private void ToggleButton_Checked( object sender, RoutedEventArgs e )
		{
		}

		private async void Button_Click( object sender, RoutedEventArgs e )
		{
			await Task.Run( () => { 
			if ( MainTask == null)
			{
				MainTask = new Thread( MainTaskTrading );
				MainTask.Start();
				((Button)sender).Foreground = new SolidColorBrush(Colors.LightGreen);
			}
			else
			{
				MainTask.Abort();
				MainTask = null;
				( (Button)sender ).Foreground = new SolidColorBrush( Colors.Red);
			}
			} );
		}

		private void Button_Click_1( object sender, RoutedEventArgs e )
		{
			main_space.Children.Clear();
			main_space.Children.Add( walletControl );
		}

		private void Button_Click_2( object sender, RoutedEventArgs e )
		{
			main_space.Children.Clear();
			main_space.Children.Add( optionsControl );
		}
	}
}
