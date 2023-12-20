using BybitMapper.UTA.RestV5.Requests.Market;
using BybitMapper.Requests;
using BybitMapper.UTA.RestV5.Data.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServerTimeResponse = BybitMapper.UTA.RestV5.Responses.Market.ServerTimeResponse;
using BybitMapper.UTA.RestV5.Responses.Market;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Kline;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Orderbook;
//using CSCommon.Http;
using BybitMapper.UTA.RestV5.Requests.Account;
using BybitMapper.UTA.RestV5.Responses.Account;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Account.WalletBalance;
using BybitMapper.UTA.UserStreamsV5;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Trade.OpenOrders;
using BybitMapper.UTA.RestV5.Requests.Trade;
using BybitMapper.UTA.RestV5.Responses.Trade;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Trade.PlaceOrder;
using System;
using System.Threading.Tasks;
using RBTB_WindowsClient.Integrations.Bybit.BybitExtensions;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Tickers.Spot;
using BybitMapper.UTA.RestV5.Responses.Market.Spot;
using Org.BouncyCastle.Asn1.Ocsp;
using BybitMapper.UTA.RestV5;
using System.Runtime.Remoting.Messaging;

namespace RBTB_WindowsClient.Integrations.Bybit;
public class BybitRestClient
{
    private RequestArranger _arranger;
    //private CommonHttpClient _client;
    private RestSharp.RestClient _restClient;
    private UtaHandlerCompositionV5 _RESTHandlers;

    private static JsonSerializerOptions jsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        IncludeFields = true
    };

    public BybitRestClient(string url)
    {
        //_client = new(url);
        _restClient = new(url);
        _arranger = new RequestArranger("api", "key");
        _RESTHandlers = new UtaHandlerCompositionV5(new UtaHandlerFactory());
    }

    public BybitRestClient(string url, string api, string key)
    {
        //_client = new(url);
        _restClient = new(url);
        _arranger = new RequestArranger(api, key);
        _RESTHandlers = new UtaHandlerCompositionV5(new UtaHandlerFactory());
    }

    #region [Base]

    internal delegate void LogDlg(string sender, string message);
    internal LogDlg? Log;
    internal bool LogResponseEnabled = false;
    internal bool LogExEnabled = false;

    void OnLogResponse(string response)
    {
        if (LogResponseEnabled)
        {
            Log?.Invoke("RestClient", string.Concat("Response: ", response));
        }
    }

    void OnLogEx(Exception ex, string? response = null)
    {
        if (LogExEnabled)
        {
            Log?.Invoke("RestClient", string.Concat("Exception: ", ex.Message, "; ", ex?.InnerException, " - ", response));
        }
    }

    #endregion

    #region [Public]

    public OrderbookResult RequestGetOrderbook(MarketCategory category, string symbol)
    {
        var request = new GetOrderbookRequest(category, symbol);
        var message = BybitHelpers.SendRestRequest<GetOrderbookResponse>(_arranger.Arrange(request), _restClient);
       
        var response = _RESTHandlers.HandleGetOrderbookResponse(message);
        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(KlineResult)));

            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));
        }

        return response.Result;
    }

    public KlineResult RequestGetKline(MarketCategory category, string symbol, IntervalType intervalType,
        DateTime? start = null, DateTime? end = null, int limit = 100)
    {
        var request = new GetKlineRequest(category, symbol, intervalType)
        {
            StartTime = start,
            EndTime = end
        };
        var message = BybitHelpers.SendRestRequest<GetKlineResponse>(_arranger.Arrange(request), _restClient);

        var response = _RESTHandlers.HandleGetKlineResponse(message);
        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(KlineResult)));

            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));
        }

        return response.Result;
    }

    public  ServerTimeData RequestGetServerTime()
    {
        var request = new ServerTimeRequest();
        var message = BybitHelpers.SendRestRequest<ServerTimeResponse>(_arranger.Arrange(request), _restClient);

        var response = _RESTHandlers.HandleServerTimeResponse(message);
        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(KlineResult)));

            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));
        }

        return response != null ? response.Result : null;
    }

    public TickerResultSpot RequestGetTickerInfo(string symbol)
    {
        var request = new GetTickersRequest(MarketCategory.Spot) { Symbol = symbol };
        var message = BybitHelpers.SendRestRequest<GetSpotTickersInfoResponse>(_arranger.Arrange(request), _restClient);

        var response = _RESTHandlers.HandleGetSpotTickersResponse(message);
        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(KlineResult)));

            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));
        }

        return response.Result;
    }

    #endregion

    #region [Private]

    public WalletBalanceResult RequestGetAccountWalletInfo()
    {
        var request = new GetWalletBalanceRequest(AccountType.Unified);
        var message = BybitHelpers.SendRestRequest<GetWalletBalanceResponse>(_arranger.Arrange(request), _restClient);

        var response = _RESTHandlers.HandleGetWalletBalanceResponse(message);
        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(WalletBalanceResult)));

            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));

            return null;
        }

        return response.Result;
    }

    public OpenOrderResult RequestGetCurrentlyOpenedOrderes(string symbol)
    {
        var request = new GetOpenOrdersRequest(MarketCategory.Spot, symbol);
        var message = BybitHelpers.SendRestRequest<GetOpenOrdersResponse>(_arranger.Arrange(request), _restClient);

        var response = _RESTHandlers.HandleGetOpenOrdersResponse(message);
        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(OpenOrderResult)));

            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));

            return null;
        }

        return response.Result;
    }

    public PlaceOrderResult RequestPlaceOrder(string _symbol,
        OrderSideType orderSideType,
        OrderType orderType,
        decimal? order_qty = null,
        decimal? triggerPrice = null,
        decimal? orderPrice = null,
        TimeInForceType? timeInForceType = null)
    {
        var request = new PlaceOrderRequest(MarketCategory.Spot, _symbol, orderSideType, orderType, order_qty, triggerPrice) { OrderPrice = orderPrice , TimeInForce = timeInForceType };
        var message = BybitHelpers.SendRestRequest<PlaceOrderResponse>(_arranger.Arrange(request), _restClient);

        var response = _RESTHandlers.HandlePlaceOrderResponse(message);
        if (response == null)
        {
            OnLogEx(new NullReferenceException(message: nameof(PlaceOrderResult)));

            return null;
        }
        if (response.RetCode != 0)
        {
            OnLogEx(new Exception(message: response.RetMsg));

            return null;
        }

        return response.Result;
    }

    #endregion
}
