using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RBTB_WindowsClient.Integrations;

public class TelegramClient
{
    private string Token { get; set; }
    private HttpClient HttpClient { get; set; }

    public TelegramClient(string token = "1993731157:AAG2GnXyAoiaTk0A9d68hcTATwGYy8YEouA")
    {
        HttpClient = new HttpClient();
        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        Token = token;
    }
	public async Task<HttpResponseMessage> SendMessage( string mess, string id ) =>
		await HttpClient
			.GetAsync( $"https://api.telegram.org/bot{Token}/sendMessage?chat_id={id}&text={mess}" );
}