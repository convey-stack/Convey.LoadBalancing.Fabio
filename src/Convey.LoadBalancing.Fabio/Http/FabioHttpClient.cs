using System.Net.Http;
using System.Threading.Tasks;
using DShop.Common.Fabio;
using Newtonsoft.Json;

namespace Convey.LoadBalancing.Fabio.Http
{
    public class FabioHttpClient : IFabioHttpClient
    {
        private readonly HttpClient _client;

        public FabioHttpClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<T> GetAsync<T>(string requestUri)
        {
            var uri = requestUri.StartsWith("http://") ? requestUri : $"http://{requestUri}";
            var response = await _client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                return default(T);
            }

            var content = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}