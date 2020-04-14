using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slipways.Mobile.Contracts
{
    public interface IBaseRepository<T>
    {
        Task<List<T>> GetAllAsync();
        Task<List<T>> GetByQueryAsync(string query);
        Task<T> GetAsync(int id);
        Task<T> GetByUuidAsync(Guid uuid);
        Task<int> InsertAsync(T entity);
        Task<int> UpdateAsync(int id, T entity);
        Task<int> DeleteAsync(T item);
        Task<int> InsertBunchAsync(IEnumerable<T> entities);
    }
}
