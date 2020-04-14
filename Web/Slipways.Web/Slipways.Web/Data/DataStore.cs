using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Web.Contracts;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Data
{
    public abstract class DataStore<T, DTO> : IDataStore<T, DTO>
        where T : class, IEntity
        where DTO : class, IDto
    {
        protected IMemoryCache _cache;
        protected IGraphQLService _graphQLService;
        private ITokenService<DTO> _service;

        protected string Key { get; set; }
        protected string Query { get; set; }
        protected string Method { get; set; }

        public DataStore(
            IMemoryCache cache,
            ITokenService<DTO> service,
            IGraphQLService graphQLService)
        {
            _cache = cache;
            _service = service;
            _graphQLService = graphQLService;
        }

        public abstract IEnumerable<T> Sort(IEnumerable<T> set);

        public virtual async Task<HashSet<T>> GetValuesAsync()
        {
            if (!_cache.TryGetValue(Key, out HashSet<T> entities))
            {
                var values = await _graphQLService.GetValuesAsync<IEnumerable<T>>(Method, Query);
                if (values == null)
                    return null;
                values = Sort(values);
                entities = values.ToHashSet();
                _cache.Set(Key, entities);
            }

            return entities;
        }

        protected async Task<DTO> InsertAsync(
            DTO item)
            => await _service.InsertAsync(item);

        public async Task<HashSet<T>> AddAsync(
            T item)
        {
            var dto = ConvertToDto(item);
            var result = await _service.InsertAsync(dto);

            if (result == null)
                return null;

            item.Id = result.Id;
            var values = await GetValuesAsync();
            values.Add(item);
            _cache.Set(Key, values);
            return values;
        }

        public abstract DTO ConvertToDto(T item);
        public abstract T ConvertToClass(DTO item);

        public async Task<HashSet<T>> RemoveAsync(
            Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (result == null)
                return null;
            var entities = await GetValuesAsync();
            entities.RemoveWhere(_ => _.Id == id);
            _cache.Set(Key, entities);
            return entities;
        }

        public async Task<HashSet<T>> UpdateAsync(
            T item, Guid id)
        {
            var dto = ConvertToDto(item);

            var result = await _service.UpdateAsync(id, dto);
            if (result == null)
                return null;

            var entities = await GetValuesAsync();
            entities.RemoveWhere(_ => _.Id == id);
            entities.Add(item);
            _cache.Set(Key, entities);
            return entities;
        }
    }
}
