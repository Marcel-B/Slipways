using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Services
{
    public class FacebookLoginService
    {
        private HttpClient _client;

        public FacebookLoginService(
            HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://www.facebook.com");
        }

        public async Task SendAsync()
        {
            var redirect = "https://slipways.de/facebook-signin";
            var clientId = "abc";
            var state = Guid.NewGuid().ToString();
            var path = $"/v5.0/dialog/oauth?client_id ={clientId}&redirect_uri ={redirect}&state={state}";
            await _client.GetAsync(path);
        }
    }
}
