using BybitMapper.Requests;
using System.Text.Json;
using System.Text.Json.Serialization;
//using CSCommon.Http;
using RestSharp;
using System;
using BybitMapper.UTA.RestV5;

namespace RBTB_WindowsClient.Integrations.Bybit.BybitExtensions;
public static class BybitHelpers
{
    public delegate void OnLogEx(Exception ex);

    public static JsonSerializerOptions jsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string SendRestRequest<T>(BybitMapper.Requests.IRequestContent message, RestClient restClient)
    {
        var request = new RestRequest(message.Query, GetHttpMethodRestSharp(message.Method));

        if (message.Body != null)
        {
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(message.Body);
        }
        if (message.Headers != null)
        {
            foreach (var header in message.Headers)
            { request.AddHeader(header.Key, header.Value); }
        }
        var result = restClient.Execute(request)?.Content;
        
        return result;
    }

    private static Method GetHttpMethodRestSharp(RequestMethod requestMethod)
    {
        switch (requestMethod)
        {
            case RequestMethod.GET: return Method.GET;
            case RequestMethod.POST: return Method.POST;
            case RequestMethod.PUT: return Method.PUT;
            case RequestMethod.DELETE: return Method.DELETE;
            default: throw new NotImplementedException("Unknown request method");
        }
    }
}
