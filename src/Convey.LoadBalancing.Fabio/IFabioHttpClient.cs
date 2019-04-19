using System.Threading.Tasks;

namespace Convey.LoadBalancing.Fabio
{
    public interface IFabioHttpClient
    {
        Task<T> GetAsync<T>(string requestUri);
    }
}