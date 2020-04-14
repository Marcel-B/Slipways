using System;
using System.Threading.Tasks;

namespace Slipways.Mobile.Contracts
{
    public interface IGraphQLService
    {
        Task<T> FetchValuesAsync<T>(string query);
    }
}
