using com.b_velop.Slipways.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Repositories
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class, IEntity
    {
        protected SlipwaysContext Context;
        protected IMemoryCache MemoryCache;
        protected ILogger<RepositoryBase<T>> Logger;
        protected readonly IList<IObserver<T>> Observers;

        protected string Key { get; set; }

        protected RepositoryBase(
            SlipwaysContext context,
            IMemoryCache memoryCache,
            ILogger<RepositoryBase<T>> logger)
        {
            Context = context;
            Observers = new List<IObserver<T>>();
            MemoryCache = memoryCache;
            Logger = logger;
        }

        public virtual async Task<IEnumerable<T>> SelectAllAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!MemoryCache.TryGetValue(Key, out HashSet<T> values))
                {
                    var fetch = await Context.Set<T>().ToListAsync(cancellationToken);
                    values = fetch.ToHashSet();
                    MemoryCache.Set(Key, values);
                }
                return values;
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while selecting values");
            }
            return default;
        }

        public virtual T SelectById(
            Guid id)
        {
            return Context.Set<T>().Find(id);
        }

        public virtual async Task<T> SelectByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentNullException("Id is null");

            try
            {
                var entities = await SelectAllAsync(cancellationToken);
                var value = entities.FirstOrDefault(_ => _.Id == id);
                return value;
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, e, $"Error occurred while selecting value by ID '{id}'");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while selecting value by ID '{id}'");
            }
            return default;
        }

        public virtual async Task<IEnumerable<T>> SelectByConditionAsync(
            Func<T, bool> expression,
            CancellationToken cancellationToken = default)
        {
            if (expression == null)
                throw new ArgumentNullException();

            try
            {
                var entities = await SelectAllAsync(cancellationToken);
                var value = entities.Where(expression);
                return value;
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, e, $"Error occurred while selecting value by condition");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while selecting value by condition");
            }
            return default;
        }

        /// <summary>
        /// Insert a new Entity into Context and update MemoryCache. Id and Created properties are set here
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Throwed when entity is null</exception>
        public virtual async Task<T> InsertAsync(
            T entity,
            CancellationToken cancellationToken = default,
            bool saveChanges = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(T));
            try
            {
                entity.Created = DateTime.Now;
                entity.Id = Guid.NewGuid();

                var result = await Context.Set<T>().AddAsync(entity, cancellationToken);

                if (saveChanges)
                    _ = Context.SaveChanges();

                var entities = await SelectAllAsync(cancellationToken);
                var list = entities.ToHashSet();
                list.Add(result.Entity);
                MemoryCache.Set(Key, list);

                foreach (var observer in Observers)
                {
                    if (result.Entity == null)
                        observer.OnError(new ArgumentNullException("No entity provided"));
                    else
                        observer.OnNext(result.Entity);
                }

                return result.Entity;
            }
            catch (DbUpdateConcurrencyException e)
            {
                Logger.LogError(6664, e, $"Error occurred while inserting new entity to database");
            }
            catch (DbUpdateException e)
            {
                Logger.LogError(6665, e, $"Error occurred while inserting new entity to database");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while inserting new entity to database");
            }
            return default;
        }

        public virtual async Task<int> InsertRangeAsync(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            bool saveChanges = true)
        {
            if (entities == null)
                throw new ArgumentNullException("Entities are null");

            try
            {
                foreach (var entity in entities)
                {
                    entity.Created = DateTime.Now;
                    foreach (var observer in Observers)
                    {
                        if (entity == null)
                            observer.OnError(new ArgumentNullException("No entity provided"));
                        else
                            observer.OnNext(entity);
                    }
                }

                await Context.Set<T>().AddRangeAsync(entities, cancellationToken);
                int count = 0;
                if (saveChanges)
                    count = Context.SaveChanges();
                var allEntities = await SelectAllAsync(cancellationToken);
                var list = allEntities.ToList();
                list.AddRange(allEntities);
                MemoryCache.Set(Key, list.ToHashSet());
                return count;
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6663, e, $"Error occurred while insert values range");
            }
            catch (DbUpdateConcurrencyException e)
            {
                Logger.LogError(6664, e, $"Error occurred while inserting new values range to database");
            }
            catch (DbUpdateException e)
            {
                Logger.LogError(6665, e, $"Error occurred while inserting new values range to database");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while insert values range");
            }
            return default;
        }

        public virtual async Task<int> UpdateRangeAsync(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            bool saveChanges = true)
        {
            if (entities == null)
                throw new ArgumentNullException("The entities were null");

            try
            {
                int cnt = 0;
                foreach (var entity in entities)
                {
                    entity.Updated = DateTime.Now;
                    var dbResult = Context.Set<T>().Update(entity);
                    if (dbResult != null)
                    {
                        cnt++;
                        foreach (var observer in Observers)
                        {
                            if (dbResult.Entity == null)
                                observer.OnError(new ArgumentNullException("No entity provided"));
                            else
                                observer.OnNext(dbResult.Entity);
                        }
                    }
                }
                if (saveChanges)
                    _ = Context.SaveChanges();
                var contextResult = await Context.Set<T>().ToListAsync(cancellationToken);
                MemoryCache.Set(Key, contextResult.ToHashSet());
                return cnt;
            }
            catch (DbUpdateConcurrencyException e)
            {
                Logger.LogError(6664, e, $"Error occurred while updating values range in database");
            }
            catch (DbUpdateException e)
            {
                Logger.LogError(6665, e, $"Error occurred while updating values range in database");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while updating values range");
            }
            return default;
        }

        public virtual async Task<T> UpdateAsync(
            T entity,
            CancellationToken cancellationToken = default,
            bool saveChanges = true)
        {
            if (entity == null)
                throw new ArgumentNullException("Entity is null");

            try
            {
                entity.Updated = DateTime.Now;
                var result = Context.Set<T>().Update(entity);
                if (saveChanges)
                    _ = Context.SaveChanges();
                var dbList = await Context.Set<T>().ToListAsync(cancellationToken);
                MemoryCache.Set(Key, dbList.ToHashSet());

                foreach (var observer in Observers)
                {
                    if (result.Entity == null)
                        observer.OnError(new ArgumentNullException("No entity provided"));
                    else
                        observer.OnNext(result.Entity);
                }

                return result.Entity;
            }
            catch (DbUpdateConcurrencyException e)
            {
                Logger.LogError(6664, e, $"Error occurred while updating value in database");
            }
            catch (DbUpdateException e)
            {
                Logger.LogError(6665, e, $"Error occurred while updating value in database");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while updating value");
            }
            return default;
        }

        public virtual async Task<T> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default,
            bool saveChanges = true)
        {
            if (id == Guid.Empty)
                throw new ArgumentNullException("Id is null");

            T result = default;
            try
            {
                var tmp = await Context.Set<T>().FirstOrDefaultAsync(_ => _.Id == id, cancellationToken);
                if (tmp == null)
                {
                    Logger.LogWarning(5000, $"Can't delete Entity with ID '{id}' - Entity not exsists");
                    return null;
                }
                result = Context.Set<T>().Remove(tmp).Entity;
                if (saveChanges)
                    _ = Context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException e)
            {
                Logger.LogError(6664, e, $"Error occurred while remove Entity with ID '{id}' from Database");
            }
            catch (DbUpdateException e)
            {
                Logger.LogError(6665, e, $"Error occurred while remove Entity with ID '{id}' from Database");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Error occurred while remove Entity with ID '{id}' from Database");
            }
            try
            {
                var entities = await SelectAllAsync(cancellationToken);
                var list = entities.ToHashSet();
                list.RemoveWhere(_ => _.Id == id);
                MemoryCache.Set(Key, list);
                return result;
            }
            catch (ArgumentNullException e)
            {
                Logger.LogError(6665, e, $"Error occurred while remove Entity with ID '{id}' from Cache");
            }
            catch (Exception e)
            {
                Logger.LogError(6666, e, $"Unexpected error occurred while remove Entity with ID '{id}' from Cache");
            }
            return null;
        }

        public IDisposable Subscribe(
            IObserver<T> observer)
        {
            if (!Observers.Contains(observer))
                Observers.Add(observer);
            return new Unsubscriber(Observers, observer);
        }

        public void UnsubscribeAll()
        {
            foreach (var observer in Observers.ToArray())
                if (Observers.Contains(observer))
                    observer.OnCompleted();
            Observers.Clear();
        }

        private class Unsubscriber : IDisposable
        {
            protected IList<IObserver<T>> Observers;
            protected IObserver<T> Observer;

            public Unsubscriber(
                IList<IObserver<T>> observers,
                IObserver<T> observer)
            {
                Observers = observers;
                Observer = observer;
            }

            public void Dispose()
            {
                if (Observers != null && Observers.Contains(Observer))
                    Observers.Remove(Observer);
            }
        }
    }
}
