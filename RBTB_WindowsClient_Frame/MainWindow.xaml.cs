using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using BinanceMapper.Requests;
using Microsoft.Win32;
using RBTB_WindowsClient.Integrations;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using RBTB_ServiceAccount.Client.Client;
using RBTB_WindowsClient_Frame.Controls;
using RBTB_WindowsClient_Frame.Integrations.MyNamespace;
using RBTB_WindowsClient_Frame.Database;
using RBTB_WindowsClient_Frame.Domains.Entities;
using BybitMapper.UTA.RestV5.Data.Enums;
using RBTB_WindowsClient.Integrations.Bybit;

namespace RBTB_WindowsClient_Frame;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private WsClientStrategyService _wsStrategyService;
    private BybitRestClient _bybitRestClient;
    private TelegramClient _telegramRestClient;
    private AccountClient _accountClient;

    private MainContext _mainContext;
    public static Dictionary<NameType, Option> _urls = new Dictionary<NameType, Option>();

    //private Guid userId = new Guid( "cf955f5a-c14c-4040-b439-38cf5736119f" ); //КО
    private Guid userId = new Guid("16348742-ccc1-4c02-9abb-8c973763b982"); //CА
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
#if Outside
			_urls = new Dictionary<NameType, Option>()
			{
				{ NameType.URL_ServiceStrategy, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "http://188.186.238.120:5246"  } },
				{ NameType.URL_ServiceAccount, new Option() { NameType = NameType.URL_ServiceAccount, ValueString = "http://188.186.238.120:5249" } },
				{ NameType.URL_Binance, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "https://api.binance.com"  } },
                { NameType.URL_Bybit, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "https://api-testnet.bybit.com"  } }
			};
#elif Debug
        _urls = new Dictionary<NameType, Option>()
        {
            { NameType.URL_ServiceStrategy, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "http://localhost:5246"  } },
            { NameType.URL_ServiceAccount, new Option() { NameType = NameType.URL_ServiceAccount, ValueString = "http://188.186.238.120:5249" } },
            { NameType.URL_Binance, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "https://api.binance.com"  } },
            { NameType.URL_Bybit, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "https://api-testnet.bybit.com"  } }
        };
#else
			_urls = new Dictionary<NameType, Option>()
			{
				{ NameType.URL_ServiceStrategy, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "http://192.168.90.213:5246"  } },
				{ NameType.URL_ServiceAccount, new Option() { NameType = NameType.URL_ServiceAccount, ValueString = "http://192.168.90.213:5249" } },
				{ NameType.URL_Binance, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "https://api.binance.com"  } },
                { NameType.URL_Bybit, new Option() { NameType = NameType.URL_ServiceStrategy, ValueString = "https://api-testnet.bybit.com"  } }
			};
#endif

        _mainContext = new MainContext();
        _mainContext.Database.CreateIfNotExists();

        _accountClient = new AccountClient(_urls[NameType.URL_ServiceAccount].ValueString, new System.Net.Http.HttpClient());

        walletControl = new WalletControl(userId, _accountClient);

        optionsControl = new OptionsControl(_mainContext);
        main_space.Children.Add(optionsControl);
        options.Foreground = new SolidColorBrush(Colors.Gray);

        _telegramRestClient = new TelegramClient();

        Log("Включение робота загрузка настроек..");
        optionsControl.logger.Document.LineHeight = 2;

        Log("Настройки успешно загружены.");
        Log("Робот готов к работе.");
    }

    private void MainTaskTrading()
    {
        Thread.Sleep(1000);

        var ra = new RequestArranger(optionsControl.Model.ApiKey, optionsControl.Model.SecretKey);
        ra.ActualityWindow = 10000;

        _bybitRestClient = new BybitRestClient(_urls[NameType.URL_Bybit].ValueString, optionsControl.Model.ApiKey, optionsControl.Model.SecretKey);
        _bybitRestClient.Log += Log;

        _wsStrategyService = new WsClientStrategyService();

        Volume = Convert.ToDecimal(optionsControl.Model.VolumeIn);
        TicksOut = Convert.ToDecimal(optionsControl.Model.PipsOut);

        SaveBalanceUSDT();

        Log($"Клиент успешно запущен\r\nВсе настройки применены\r\nОбъем входа: {Volume}\r\nТорговля: {IsTrading}\r\nПунктов на выход: {TicksOut}");
        Log($"Подключение к серверу стратегий..");

        _wsStrategyService.StrategyTradeEv += WsStrategyServiceOnStrategyTradeEv;
        _wsStrategyService.ErrorEvent += WsStrategyServiceOnErrorEv;
        _wsStrategyService.OpenEvent += WsStrategyServiceOnOpenEv;
        _wsStrategyService.SetUrlServiceStrategy(_urls[NameType.URL_ServiceStrategy].ValueString);
        _wsStrategyService.Start();
    }

    private void SaveBalanceUSDT()
    {
        var balance = _bybitRestClient.RequestGetAccountWalletInfo();
        if (balance != null)
        {
            var balances = balance.List[0].Coins
            .Where(x => x.WalletBalance != 0)
            .ToList();
            var usdt_balance = 0.0m;
            foreach (var item in balances)
            {

                if (item.Name == "USDT")
                {
                    usdt_balance += item.WalletBalance ?? 0;
                    continue;
                }
                var ticker = _bybitRestClient.RequestGetTickerInfo(item.Name + "USDT");
                if (ticker != null)
                {
                    var price = ticker.List[0].LastPrice;
                    usdt_balance += item.WalletBalance ?? item.WalletBalance * price ?? 0;
                }
            }

            _accountClient.Create4Async(new CreateWalletRequest() { UserId = userId, Symbol = "USDT", Balance = Convert.ToDouble(usdt_balance), Market = "Binance" });

            Log("Баланс USDT: " + usdt_balance.ToString());
        }
    }

    private async void MainWindow_OnClosing(object sender, CancelEventArgs e) => await optionsControl.SavingOptions(optionsControl);
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
        if (UseLevels.Contains(level))
            return;

        //Log($"Найдена ситуация для торговли\r\nУровень: {level}, Цена: {price}, Инструмент: {symbol}");
        var openedOrders = _bybitRestClient.RequestGetCurrentlyOpenedOrderes(symbol);
        if (openedOrders != null)
        {
            if (openedOrders.List.Count != 0)
            {
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
            Log("Сигнал торговли, открываю ордера..");
            SaveBalanceUSDT();

            BuyMarketSellLimit(price, symbol, level);
        }
        else
        { Log("Автоматическая торговля запрещена.."); }
        UseLevels.Add(level);
        Thread.Sleep(5000);
    }

    private void BuyMarketSellLimit(decimal price, string symbol, decimal level)
    {
        var order = _bybitRestClient.RequestPlaceOrder(symbol, OrderSideType.Buy, BybitMapper.UTA.RestV5.Data.Enums.OrderType.Market, order_qty: Volume * price);
        if (order != null)
        {
            Log($"Выставлена покупка {level}");
        }
        else
        {
            Log($"Ошибка открытия ордера покупки\r\n");
        }

        var orderOut = _bybitRestClient.RequestPlaceOrder(symbol, OrderSideType.Sell,
            BybitMapper.UTA.RestV5.Data.Enums.OrderType.Limit,
            order_qty: Volume,
            timeInForceType: TimeInForceType.GTC,
            orderPrice: price + TicksOut);
        if (orderOut != null)
        {
            Log($"Выставлена продажа {price + TicksOut}");
        }
        else
        {
            Log($"Ошибка открытия ордера продажи\r\n");
        }
    }

    private async void Log(string text)
    {
        optionsControl.Model.Log = optionsControl.Model.Log.Insert(0, $"[{DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()}] " + text + Environment.NewLine);

        var res = await _telegramRestClient.SendMessage(text, optionsControl.Model.TelegramId);
        if (res != null)
        {
            if (res.StatusCode == System.Net.HttpStatusCode.BadRequest || res.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                var tgtext = await res.Content.ReadAsStringAsync();
                optionsControl.Model.Log = optionsControl.Model.Log.Insert(0, $"[{DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()}] Ошибка подключения Телеграм!\r\n{tgtext}");
            }
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

        optionsControl.SecretKey.Text = ofd.FileName;
    }

    private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        MainTask.Abort();
    }

    private void ToggleButton_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (MainTask == null)
        {
            power.Foreground = new SolidColorBrush(Colors.LightGreen);
        }
        else
        {
            power.Foreground = new SolidColorBrush(Colors.Red);
        }

        if (MainTask == null)
        {
            MainTask = new Thread(MainTaskTrading);
            MainTask.Start();
        }
        else
        {
            MainTask.Abort();
            MainTask = null;
        }

    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        main_space.Children.Clear();
        main_space.Children.Add(walletControl);
        charts.Foreground = new SolidColorBrush(Colors.Gray);
        options.Foreground = new SolidColorBrush(Colors.White);
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        main_space.Children.Clear();
        main_space.Children.Add(optionsControl);
        options.Foreground = new SolidColorBrush(Colors.Gray);
        charts.Foreground = new SolidColorBrush(Colors.White);
    }
}
