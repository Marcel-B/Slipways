using Slipways.Mobile.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slipways.Mobile.Data.Repositories
{

    public class BaseRepository<T> : IBaseRepository<T> where T : IEntity, new()
    {
        protected IDataContext Context { get; }
        protected List<T> Cache { get; set; }

        public BaseRepository(
            IDataContext context)
        {
            Context = context;
        }

        public async virtual Task<List<T>> GetAllAsync()
        {
            if (Cache == null || Cache.Count() == 0)
            {
                Cache = new List<T>();
                Cache = await Context.Table<T>().ToListAsync();
            }
            return Cache;
        }

        public async Task<List<T>> GetByQueryAsync(string query)
            // SQL queries are also possible
            => await Context.QueryAsync<T>(query);// "SELECT * FROM [TodoItem] WHERE [Done] = 0"

        public async Task<T> GetAsync(
            int id)
            => await Context.Table<T>()
            .Where(_ => _.Id == id)
            .FirstOrDefaultAsync();

        public async Task<T> GetByUuidAsync(
            Guid uuid)
           => await Context
            .Table<T>()
            .Where(_ => _.Pk == uuid)
            .FirstOrDefaultAsync();

        public async Task<int> InsertAsync(
            T entity)
            => await Context.InsertAsync(entity);

        public async Task<int> UpdateAsync(
            int id, T entity)
            => await Context.UpdateAsync(entity);

        public async Task<int> DeleteAsync(
            T item)
            => await Context.DeleteAsync(item);

        public async Task<int> InsertBunchAsync(
            IEnumerable<T> entities)
            => await Context.InsertBunchAsync<T>(entities);

    }
}
