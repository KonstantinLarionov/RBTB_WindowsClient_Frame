using System;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using WebSocketSharp;

namespace RBTB_WindowsClient.Integrations;

public class StrategyTradeResponse
{
    public decimal Price { get; set; }
    public string Symbol { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Level { get; set; }
}

public class WsClientStrategyService
{
    private string _baseUrl = "wss://localhost:32768";
    private WebSocket _socket;
    private int _reconnectCounter = 40;
    private int _WSreconnectCounter;

    public delegate void StrategyTradeHandler(decimal price, string symbol, decimal level, DateTime dateTime);
    public event StrategyTradeHandler StrategyTradeEv;
    
    public delegate void OpenHandler(EventArgs e);
    public event OpenHandler OpenEvent;
    public delegate void CloseHandler(EventArgs e);
    public event CloseHandler CloseEvent;
    public delegate void ErrorHandler(ErrorEventArgs e, int countconnect = 0, bool reconnect = false);
    public event ErrorHandler ErrorEvent;
    
    public WsClientStrategyService(int countReconnect = 40)
    {
        _reconnectCounter = countReconnect;
    }

    public void SetUrlServiceStrategy(string base_url) => _baseUrl = base_url;

    public void Start()
    {
         _socket = new WebSocket(_baseUrl.Replace("http", "ws") + "/ws/subscribe?idUser=" + Guid.NewGuid());
        _socket.OnClose += _socket_OnClose;
        _socket.OnError += _socket_OnError;
        _socket.OnMessage += _socket_OnMessage;
        _socket.OnOpen += _socket_OnOpen;
        _socket.Connect();
        
        _socket.Send("trade");
    }

    private void _socket_OnOpen(object sender, EventArgs e)
    {
        _WSreconnectCounter = _reconnectCounter;

        OpenEvent?.Invoke(e);
    }

    private void _socket_OnMessage(object sender, MessageEventArgs e)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<StrategyTradeResponse>(e.Data);
            StrategyTradeEv?.Invoke(message.Price, message.Symbol, message.Level, message.DateTime);
        }
        catch (Exception)
        {
        }
    }

    public void Subscribe() => _socket.Send("trade");

    public void Ping()
    {
        _socket.Send(Encoding.UTF8.GetBytes("ping"));
    }

    private void _socket_OnError(object sender, ErrorEventArgs e)
    {
        if (!_socket.IsAlive)
        {
            ErrorEvent?.Invoke(e, _WSreconnectCounter, true);
        }
        else
            ErrorEvent?.Invoke(e);
    }

    private void _socket_OnClose(object sender, CloseEventArgs e)
    {
        if(!e.WasClean)
        {
            if(!_socket.IsAlive && _WSreconnectCounter > 0)
            {
                _WSreconnectCounter--;

                Thread.Sleep(1500);
                _socket.Connect();

                CloseEvent?.Invoke(e);
            }
        }

        CloseEvent?.Invoke(e);
    }

    public void Stop() => _socket?.Close();
}