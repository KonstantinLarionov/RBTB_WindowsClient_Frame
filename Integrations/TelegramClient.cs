using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RBTB_WindowsClient.Integrations;

public class TelegramClient
{
    private string Token { get; set; }
    private HttpClient HttpClient { get; set; }

    public TelegramClient(string token = "6072379432:AAFGUfuPxwu6l6rTgsozMiNwHxZGx44mdTM")
    {
        HttpClient = new HttpClient();
        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        Token = token;
    }
	public async Task<HttpResponseMessage> SendMessage( string mess, string id ) =>
		await HttpClient
			.GetAsync( $"https://api.telegram.org/bot{Token}/sendMessage?chat_id={id}&text={mess}" );
}