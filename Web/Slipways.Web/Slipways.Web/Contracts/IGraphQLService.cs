using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Contracts
{
    public interface IGraphQLService
    {
        Task<T> GetValuesAsync<T>(string method, string query);
    }
}