
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using BinanceMapper.Requests;
using BinanceMapper.Spot.Exchange.V3;
using BinanceMapper.Spot.Exchange.V3.Data;
using BinanceMapper.Spot.Exchange.V3.Requests;
using BinanceMapper.Spot.Exchange.V3.Responses;
using BinanceMapper.Spot.Margin.V1.Responses;
using RestSharp;
using AccountInfoRequest = BinanceMapper.Spot.Exchange.V3.Requests.AccountInfoRequest;
using CloseListenKeyRequest = BinanceMapper.Spot.Exchange.V3.Requests.CloseListenKeyRequest;
using CurrentlyOpenedOrdersRequest = BinanceMapper.Spot.Exchange.V3.Requests.CurrentlyOpenedOrdersRequest;
using ErrorMessage = BinanceMapper.Spot.Exchange.V3.Responses.ErrorMessage;
using ListenKeyData = BinanceMapper.Spot.Exchange.V3.Responses.ListenKeyData;
using ListenKeyRequest = BinanceMapper.Spot.Exchange.V3.Requests.ListenKeyRequest;
using NewOrderRequest = BinanceMapper.Spot.Exchange.V3.Requests.NewOrderRequest;
using OrderCancelRequest = BinanceMapper.Spot.Exchange.V3.Requests.OrderCancelRequest;
using OrderCancelResult = BinanceMapper.Spot.Exchange.V3.Responses.OrderCancelResult;
using OrderResult = BinanceMapper.Spot.Exchange.V3.Responses.OrderResult;
using OrderStatusData = BinanceMapper.Spot.Exchange.V3.Responses.OrderStatusData;
using Trade = BinanceMapper.Spot.Exchange.V3.Responses.Trade;
using TradesRequest = BinanceMapper.Spot.Exchange.V3.Requests.TradesRequest;
using VoidResponse = BinanceMapper.Spot.Exchange.V3.Responses.VoidResponse;

namespace RBTB_WindowsClient.Integrations.Binance
{
    public class BinanceRestClient
    {
       readonly ExchangeApiV3HandlerComposition m_HandlerComposition;
        internal ExchangeApiV3HandlerComposition HandlerComposition => m_HandlerComposition;

        readonly RequestArranger m_RequestsArranger;

        /// <summary>
        /// =======
        /// </summary>
        internal BinanceRestClient(RequestArranger ra)
        {
            m_HandlerComposition = new ExchangeApiV3HandlerComposition(new ExchangeApiV3HandlerFactory());
            m_RequestsArranger = ra;
         
        }

        #region [Base]

        RestClient m_RestClient;
        /// <summary>
        /// =======
        /// </summary>
        internal void SetUrl(string rest_url)
        {
            m_RestClient = new RestClient(rest_url);
        }
        /// <summary>
        /// =======
        /// </summary>
        internal void SetProxy(string ip, string port, string username = null, string password = null)
        {
            try
            {
                var proxy = new WebProxy($"{ip}:{port}");
                if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
                { proxy.Credentials = new NetworkCredential(username, password); }
            }
            catch(Exception ex)
            { OnLogEx(ex); }
        }

        internal delegate void Log_Dlg(string sender, string message);
        internal Log_Dlg Log;
        internal bool LogResponceEnabled = false;
        internal bool LogExEnabled = false;
        /// <summary>
        /// =======
        /// </summary>
        void OnLogResponce(string responce)
        {
            if (LogResponceEnabled)
            { Log?.Invoke("RestClient", string.Concat("Responce: ", responce)); }
        }
        /// <summary>
        /// =======
        /// </summary>
        void OnLogEx(Exception ex, string responce = null)
        {
            if (LogExEnabled)
            { Log?.Invoke("RestClient", string.Concat("Exception: ", ex.Message, "; ", ex?.InnerException, " - ", responce)); }
        }

        /// <summary>
        /// =======
        /// </summary>
        string SendRestRequest(IRequestContent message)
        {
            Method method;

            switch (message.Method)
            {
                case RequestMethod.GET:
                    method = Method.GET;
                    break;
                case RequestMethod.POST:
                    method = Method.POST;
                    break;
                case RequestMethod.PUT:
                    method = Method.PUT;
                    break;
                case RequestMethod.DELETE:
                    method = Method.DELETE;
                    break;
                default:
                    throw new NotImplementedException("Unknown request method");
            }

            var request = new RestRequest(message.Query, method);

            if (message.Headers != null)
            {
                foreach (var header in message.Headers)
                { request.AddHeader(header.Key, header.Value); }
            }
            //OnLogResponce(message.Query);//test
            var r = m_RestClient.Execute(request);
            return r.Content;
        }

        #endregion

        #region [Requests (public)]

        /// <summary>
        /// =======
        /// </summary>
        internal bool RequestExchangeInfo(out ExchangeInfo data)
        {
            data = null;

            var request = m_RequestsArranger.Arrange(new ExchangeInfoRequest());

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleExchangeInfo(responce);
                //OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }
        internal bool RequestTickerInfo(string symbol, out DailyTickerPriceChange data)//#spot_watchlist
        {
            // #watch_list
            data = null;
            var request_data = new DailyTickerPriceChangeRequest();
            request_data.Symbol = symbol;
            
            var request = m_RequestsArranger.Arrange(request_data);
          
            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleDailyTickerPriceChange(responce);
                //OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }

        /// <summary>
        /// =======
        /// </summary>
        internal bool RequestServerTime(out ServerTimeData data)
        {
            data = null;

            var request = m_RequestsArranger.Arrange(new ServerTimeRequest());

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleServerTime(responce);
                //OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }
        /// <summary>
        /// =======
        /// </summary>
        internal bool RequestOrderBook(out OrderBookData data, string symbol, int limit = 1000)
        {
            data = null;

            var request = m_RequestsArranger.Arrange(new OrderBookRequest(symbol) { Limit = limit });

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleOrderBook(responce);
                //OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }
        /// <summary>
        /// =======
        /// </summary>
        internal bool RequestCandles(out IReadOnlyList<Candle> data,
            string symbol, CandleInterval timeframe,
            DateTime? start = null, DateTime? end = null, int limit = 100)
        {
            //* Binance limit: max=1000, default=100

            data = null;

            var request_content = new CandlesRequest(symbol, timeframe)
            {
                Start = start,
                End = end,
                Limit = Math.Min(limit, 1000)
            };
            var request = m_RequestsArranger.Arrange(request_content);

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleCandles(responce);
                //OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }

        #endregion

        #region [Requests (user)]

        //TODO:
        //
        //-операции с заявками; запрос сделок (здесь или в первой версии?)
        //
        //*Not implemented:
        //CurrentAveragePriceRequest
        //OrderBookTickerRequest
        //OrderStatusRequest
        //PriceTickerRequest

        /// <summary>
        /// 
        /// </summary>
        bool RequestTemplate(/*out XXX data*/)
        {
            //*Not implemented:
            //AllOrdersRequest
            //CurrentAveragePriceRequest
            //CurrentlyOpenedOrdersRequest
            //NewOrderRequest
            //OrderBookTickerRequest
            //OrderCancelRequest
            //OrderStatusRequest
            //PriceTickerRequest
            //TradesRequest

            //data = null;

            //var request = m_RequestsArranger.Arrange(new XXX());

            //string responce = string.Empty;
            //try
            //{
            //    responce = SendRestRequest(request);
            //    data = m_HandlerComposition.HandleXXX(responce);
            //    OnLogResponce(responce);
            //    return true;
            //}
            //catch (Exception ex)
            //{ OnLogEx(ex, responce); }
            return false;
        }

        /// <summary>
        /// =======
        /// </summary>
        internal bool RequestListenKey(out ListenKeyData data, out ErrorMessage error_responce)
        {
            //TODO
            //ExtendListenKeyRequest

            data = null;
            error_responce = null;

            var request = m_RequestsArranger.Arrange(new ListenKeyRequest());

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleListenKey(responce);
                //OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is FormatException)
                {
                    try
                    {
                        error_responce = m_HandlerComposition.HandleErrorMessage(responce);
                        OnLogResponce(responce);//нормальный json ответ биржи об отклонении команды
                    }
                    catch (Exception handle_ex)//ошибка обработки ответа об ошибке
                    { OnLogEx(handle_ex, responce); }
                }
                else//другая ошибка
                { OnLogEx(ex, responce); }
            }
            return false;
        }
        /// <summary>
        /// =======
        /// </summary>
        internal bool RequestCloseListenKey(out VoidResponse data, string listen_key)
        {
            data = default(VoidResponse);
            
            var request = m_RequestsArranger.Arrange(new CloseListenKeyRequest(listen_key));

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleVoidResponse(responce);
                OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }

        /// <summary>
        /// Get current account information
        /// </summary>
        internal bool RequestAccountInfo(out AccountInfo data)
        {
            data = null;

            var request = m_RequestsArranger.Arrange(new AccountInfoRequest());
            
            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleAccountInfo(responce);
                //OnLogResponce(responce);//много инфы, захламляет лог
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }

        /// <summary>
        /// Get trades for a specific account and symbol
        /// </summary>
        internal bool RequestTrades(out IReadOnlyList<Trade> data,
            string symbol,
            int? limit = null, long? from_id = null,
            DateTime? start = null, DateTime? end = null)
        {
            //Get trades for a specific account and symbol.
            //Weight: 5 with symbol.
            //
            //FromID - TradeId to fetch from. Default gets most recent trades. 
            //If FromID is set, it will get orders >= that fromId. Otherwise most recent orders are returned.
            //Limit - Default 500; max 1000.

            data = null;

            var request_content = new TradesRequest(symbol);

            if (string.IsNullOrEmpty(symbol))
            { return false; }

            if (limit.HasValue)
            { request_content.Limit = limit.Value; }
            else
            { request_content.Limit = 500; }

            if (from_id.HasValue)
            { request_content.FromID = from_id.Value; }

            if (start.HasValue)
            { request_content.Start = start.Value; }

            if (end.HasValue)
            { request_content.End = end.Value; }

            //request_content.StartInMilliseconds
            //request_content.EndInMilliseconds

            var request = m_RequestsArranger.Arrange(request_content);

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleTrades(responce);
                if (data.Count > 0)
                { OnLogResponce(responce); }
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }

            return false;
        }

        /// <summary>
        /// Get all account orders: active, canceled, or filled
        /// </summary>
        internal bool RequestAllOrders(out IReadOnlyList<OrderStatusData> data,
            string symbol,
            int? limit = null, long? from_id = null,
            DateTime? start = null, DateTime? end = null)
        {
            //Get all account orders; active, canceled, or filled.
            //Weight: 5 with symbol.
            //
            //Limit - Default 500; max 1000.
            //If OrderId is set, it will get orders >= than OrderId. Otherwise most recent orders are returned.
            //For some historical orders cummulativeQuoteQty will be < 0, meaning the data is not available at this time.

            data = null;

            var request_content = new AllOrdersRequest(symbol);

            if (string.IsNullOrEmpty(symbol))
            { return false; }

            if (limit.HasValue)
            { request_content.Limit = limit.Value; }
            else
            { request_content.Limit = 500; }

            if (from_id.HasValue)
            { request_content.FromID = from_id.Value; }

            if (start.HasValue)
            { request_content.Start = start.Value; }

            if (end.HasValue)
            { request_content.End = end.Value; }

            //request_content.StartInMilliseconds
            //request_content.EndInMilliseconds

            var request = m_RequestsArranger.Arrange(request_content);

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleOrdersStatusData(responce);
                if (data.Count > 0)
                { OnLogResponce(responce); }
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }

        /// <summary>
        /// Get all opened orders on a symbol
        /// </summary>
        internal bool RequestCurrentlyOpenedOrders(out IReadOnlyList<OrderStatusData> data, 
            string symbol = null)
        {
            //Get all opened orders on a symbol. Careful when accessing this with no symbol.
            //Weight: 1 for a single symbol; 40 when the symbol parameter is omitted.
            //
            //If the symbol is not sent, orders for all symbols will be returned in an array.
            //When all symbols are returned, the number of requests counted against the rate limiter is equal to the number of symbols currently trading on the exchange.

            data = null;

            var request_content = new CurrentlyOpenedOrdersRequest();

            if (!string.IsNullOrEmpty(symbol))
            { request_content.Symbol = symbol; }

            var request = m_RequestsArranger.Arrange(request_content);

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleOrdersStatusData(responce);
                OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            { OnLogEx(ex, responce); }
            return false;
        }

        const int m_iMaxClientOrderIdLength = 32;
        /// <summary>
        /// Send in a new order
        /// </summary>
        internal bool RequestNewOrder(out OrderResult data, out ErrorMessage error_responce,
             string sign,
            NewOrderRequest request_content)
        {
            data = null;
            error_responce = null;

            /*
                The broker ID should be sent as the initial part in the ""newClientOrderId"" when your client places an order so that our will consider the order as a brokerage order.
                https://binance-docs.github.io/apidocs/spot/en/#new-order-trade"
                For example, if your broker ID is  “ABC123”, the "newClientOrderId" should be started with "x-ABC123" 
                when any of your clients places an order, which means if  the client's original client order id is created as "qwer1234",
                the "newClientOrderId" should be "x-ABC123qwer1234" when the order placed.
                And the clientOrderId cannot have more than 32 characters.
            */
            //.OrderResponseType = OrderResponseType.Full; //ACK, RESULT, or FULL; MARKET and LIMIT order types default to FULL, all other orders default to ACK. //<<<
            //.ClientOrderID //A unique id for the order. Automatically generated if not sent.
            //.IcebergQuantity //Used with LIMIT, STOP_LOSS_LIMIT, and TAKE_PROFIT_LIMIT to create an iceberg order.
            //.IsTest
            //.StopPrice //Used with STOP_LOSS, STOP_LOSS_LIMIT, TAKE_PROFIT, and TAKE_PROFIT_LIMIT orders.

            var request = m_RequestsArranger.Arrange(request_content);

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleOrderResult(responce);//<<<
                OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is FormatException)//#orderopex
                {
                    try
                    {
                        error_responce = m_HandlerComposition.HandleErrorMessage(responce);
                        OnLogResponce(responce);//нормальный json ответ биржи об отклонении команды
                    }
                    catch (Exception handle_ex)//ошибка обработки ответа об ошибке
                    { OnLogEx(handle_ex, responce); }
                }
                else//другая ошибка
                { OnLogEx(ex, responce); }
            }
            return false;
        }

        /// <summary>
        /// Cancel an active order
        /// </summary>
        internal bool RequestCancelOrder(out OrderCancelResult data, out ErrorMessage error_responce,
            string symbol, long order_id)
        {
            data = null;
            error_responce = null;

            var request_content = new OrderCancelRequest(symbol, order_id);

            //.ClientOrderID //Either OrderId or OrigClientOrderID must be sent.
            //.ClientCancelID //Used to uniquely identify this cancel. Automatically generated by default.

            var request = m_RequestsArranger.Arrange(request_content);

            string responce = string.Empty;
            try
            {
                responce = SendRestRequest(request);
                data = m_HandlerComposition.HandleOrderCancelResult(responce);
                OnLogResponce(responce);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is FormatException)//#orderopex
                {
                    try
                    {
                        error_responce = m_HandlerComposition.HandleErrorMessage(responce);
                        OnLogResponce(responce);//нормальный json ответ биржи об отклонении команды
                    }
                    catch (Exception handle_ex)//ошибка обработки ответа об ошибке
                    { OnLogEx(handle_ex, responce); }
                }
                else//другая ошибка
                { OnLogEx(ex, responce); }
            }
            return false;
        }

        #endregion
    }
}