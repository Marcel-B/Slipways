using Prism.Events;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using Slipways.Mobile.Events;
using Slipways.Mobile.Infrastructure;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slipways.Mobile.Data
{
    public class DataContext : IDataContext
    {
        public SQLiteAsyncConnection Context { get; }
        private readonly InitializationReadyEvent _initializationReadyEvent;

        private bool _initialized = false;
        public AsyncTableQuery<T> Table<T>() where T : IEntity, new() => Context.Table<T>();
        public Task<List<T>> QueryAsync<T>(string query) where T : IEntity, new() => Context.QueryAsync<T>(query);
        public Task<int> InsertAsync<T>(T entity) where T : IEntity, new() => Context.InsertAsync(entity);
        public Task<int> UpdateAsync<T>(T entity) where T : IEntity, new() => Context.UpdateAsync(entity);
        public Task<int> DeleteAsync<T>(T entity) where T : IEntity, new() => Context.DeleteAsync<T>(entity);

        public async Task<int> InsertBunchAsync<T>(IEnumerable<T> data) where T : IEntity, new()
        {
            var result = await Context.InsertAllAsync(data, true);
            return result;
        }

        public DataContext(
            IEventAggregator eventAggregator)
        {
            Context = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            _initializationReadyEvent = eventAggregator.GetEvent<InitializationReadyEvent>();
        }

        public async Task Initialize()
        {
            if (!_initialized)
            {
                await Context.RunInTransactionAsync(tran =>
               {
                   var mappings = tran.TableMappings;

                   tran.CreateTable<Water>(CreateFlags.None);
                   tran.CreateTable<SlipwayExtra>(CreateFlags.None);
                   tran.CreateTable<Slipway>(CreateFlags.None);
                   tran.CreateTable<Manufacturer>(CreateFlags.None);
                   tran.CreateTable<Extra>(CreateFlags.None);
                   tran.CreateTable<Marina>(CreateFlags.None);
                   tran.CreateTable<Service>(CreateFlags.None);
                   tran.CreateTable<Station>(CreateFlags.None);
                   //if (!mappings.Any(m => m.MappedType.Name == typeof(User).Name))
                   //{
                   //    await Context.CreateTableAsync<User>(CreateFlags.None);
                   //    var user = new User
                   //    {
                   //        Created = DateTime.Now,
                   //        Pk = Guid.NewGuid(),
                   //        Name = "John Doe",
                   //        Version = "1.0"
                   //    };
                   //    await Context.InsertAsync(user);
                   //}
                   //else
                   //{

                   //}
               });
               // .ContinueWith(_ =>
               //{
               //     // Database now ready - check for updates
                   
               //});
                _initialized = true;
                _initializationReadyEvent.Publish(_initialized);
            }

        }
    }
}
