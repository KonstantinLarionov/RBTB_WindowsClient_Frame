using BybitMapper.Requests;
using System.Text.Json;
using System.Text.Json.Serialization;
//using CSCommon.Http;
using RestSharp;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Org.BouncyCastle.Ocsp;

namespace RBTB_WindowsClient.Integrations.Bybit.BybitExtensions;
public static class BybitHelpers
{
    public static JsonSerializerOptions jsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    //internal static async Task<T?> GetContentAsync<T>(RequestPayload request, RequestArranger arranger, CommonHttpClient client)
    //{
    //    var arrange = arranger.Arrange(request);

    //    var c = await client.GetContentAsyncString(GetHttpMethod(arrange.Method), arrange.Query, null!,null!);

    //    T? content;
    //    try
    //    {
    //        content = client.GetContent<T>(GetHttpMethod(arrange.Method), arrange.Query, null!, null!);
    //    }
    //    catch (Exception)
    //    {

    //        return default(T);
    //    }

    //    return content;
    //}

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

    private static HttpMethod GetHttpMethod(RequestMethod method)
    {
        switch (method)
        {
            case RequestMethod.GET: return HttpMethod.Get;
            case RequestMethod.POST: return HttpMethod.Post;
            case RequestMethod.PUT: return HttpMethod.Put;
            case RequestMethod.DELETE: return HttpMethod.Delete;
            default: throw new NotImplementedException();
        }
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
