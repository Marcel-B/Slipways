using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slipways.Mobile.Contracts
{
    public interface IDataContext
    {
        SQLiteAsyncConnection Context { get; }
        AsyncTableQuery<T> Table<T>() where T : IEntity, new();
        Task<List<T>> QueryAsync<T>(string query) where T : IEntity, new();
        Task<int> InsertAsync<T>(T entity) where T : IEntity, new();
        Task<int> UpdateAsync<T>(T entity) where T : IEntity, new();
        Task<int> DeleteAsync<T>(T entity) where T : IEntity, new();
        Task<int> InsertBunchAsync<T>(IEnumerable<T> data) where T : IEntity, new();
        Task Initialize();
    }
}
