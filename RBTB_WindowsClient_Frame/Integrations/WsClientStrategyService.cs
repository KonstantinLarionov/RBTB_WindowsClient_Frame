using System;
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
    
    public delegate void StrategyTradeHandler(decimal price, string symbol, decimal level, DateTime dateTime);
    public event StrategyTradeHandler StrategyTradeEv;
    
    public delegate void OpenHandler(EventArgs e);
    public event OpenHandler OpenEvent;
    public delegate void ErrorHandler(ErrorEventArgs e);
    public event ErrorHandler ErrorEvent;
    
    public WsClientStrategyService()
    {
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

    private void _socket_OnError(object sender, ErrorEventArgs e)
    {
        ErrorEvent?.Invoke(e);
    }

    private void _socket_OnClose(object sender, CloseEventArgs e)
    {
    }

    public void Stop() => _socket?.Close();
}