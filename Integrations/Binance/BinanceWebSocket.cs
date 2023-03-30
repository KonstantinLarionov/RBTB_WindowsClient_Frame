using System;
using BinanceMapper.Spot.MarketWS;
using BinanceMapper.Spot.MarketWS.Data.Enums;
using BinanceMapper.Spot.MarketWS.Events;
using BinanceMapper.Spot.MarketWS.Subscriptions;
using BinanceMapper.Spot.UserStream;
using BinanceMapper.Spot.UserStream.Events;
using BinanceMapper.Spot.Websocket;
using BinanceMapper.Spot.Websocket.Data;
using BinanceMapper.Spot.WebSocket.Events;
using WebSocketSharp;

namespace RBTB_WindowsClient.Integrations.Binance
{
    public class BinanceWebSocket
    {
        private WebSocket _socket; 
        internal BinanceMapper.Spot.MarketWS.MarketStreamsSpotHandlerComposition DeliveryPublic { get; }
        
        internal BinanceMapper.Spot.Websocket.WebsocketApiV1HandlerComposition SpotPublic { get; }
        internal BinanceMapper.Spot.UserStream.UserStreamApiV1HandlerComposition SpotUser { get; }
        
        public string Symbol { get; set; }
        public delegate void DepthEvent(OrderBookEvent bookEvent);
        public event DepthEvent DepthEv;
        public delegate void TickEvent(SymbolTickerEvent tickEvent);
        public event TickEvent TickEv;
        public delegate void TradesEvent(TradeEvent tradesEvent);
        public event TradesEvent TradeEv;
        public delegate void ExecEvent(UserStreamEvent exec);
        public event ExecEvent ExecEv;
        public delegate void UserEvent(UserStreamEvent exec);
        public event UserEvent UserEv;

        public BinanceWebSocket()
        {
            DeliveryPublic = new MarketStreamsSpotHandlerComposition(new MarketStreamsSpotHandlerFactory());
            SpotPublic = new WebsocketApiV1HandlerComposition(new WebSocketApiV1HandlerFactory());
            SpotUser = new UserStreamApiV1HandlerComposition(new UserStreamApiV1HandlerFactory());
        }

        private void SocketOnOnMessage(object sender, MessageEventArgs e)
        {
            DefaultPublicSpotEvent base_event = null;
            DefaultEvent def_event = null;
            try
            {
                base_event = DeliveryPublic.HandleDefaultPublicSpotEvent(e.Data);
            }
            catch
            {
            }

            try
            {
                def_event = SpotPublic.HandleDefaultEvent(e.Data);
            }
            catch
            {
            }

            if (base_event != null)
            {
                if (base_event.EventType == EventSpotPublicType.OrderBook)
                {
                    var data = DeliveryPublic.HandleOrderBookEvent(e.Data);
                    DepthEv?.Invoke(data);
                }
                else if (base_event.EventType == EventSpotPublicType.Ticker)
                {
                    var data = DeliveryPublic.HandleSymbolTickerEvent(e.Data);
                    TickEv?.Invoke(data);
                }
                else if (base_event.EventType == EventSpotPublicType.Trade)
                {
                    var data = DeliveryPublic.HandleTradeEvent(e.Data);
                    TradeEv?.Invoke(data);
                }
            }

            if (def_event != null)
            {
                if (def_event.EventType == WebsocketEventType.ExecutionReport)
                {
                    var us_event = SpotUser.HandleUserStreamEvent(e.Data);
                    ExecEv?.Invoke(us_event);
                }

                else if (def_event.EventType == WebsocketEventType.AccountInfo)
                {
                    var us_event = SpotUser.HandleUserStreamEvent(e.Data);
                    UserEv?.Invoke(us_event);
                }
            }
        }

        public string SubTick()
        {
            var cmd = SpotMarketCombineStreamsSubs.CreatePublicSub(Symbol,
                BinanceMapper.Spot.MarketWS.Data.Enums.MarketSpotSubType.Subscribe,
                BinanceMapper.Spot.MarketWS.Data.Enums.PublicSpotEndpointType.Trade);
            return cmd;
        }
        public string SubDepth()
        {
            // var cmd = DeliveryMarketCombineStreamsSubs.CreatePublicSub("BTCUSDT",
            //     BinanceMapper.Delivery.MarketWS.Data.Enums.MarketDeliverySubType.Subscribe,
            //     BinanceMapper.Delivery.MarketWS.Data.Enums.PublicDeliveryEndpointType.OrderBook);
            // cmd = cmd.Replace("@depth", "@depth@100ms");
            // return cmd;
            return null;
        }

        public void Start(string liskey)
        {
            _socket = new WebSocket(liskey);
            _socket.OnMessage += SocketOnOnMessage;
            _socket.Connect();
            _socket.Send(SubTick());
            _socket.OnError += SocketOnOnError;
            _socket.OnClose += SocketOnOnClose;
        }

        private void SocketOnOnClose(object sender, CloseEventArgs e)
        {
        }

        private void SocketOnOnError(object sender, ErrorEventArgs e)
        {
        }

        public void Stop()
        {
            _socket.Close();
        }
    }
}