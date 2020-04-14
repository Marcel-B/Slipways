using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Contracts;

namespace com.b_velop.Slipways.Web.Contracts
{
    public interface IDataStore<T, DTO>
        where T : class, IEntity
        where DTO : IDto
    {
        Task<HashSet<T>> GetValuesAsync();
        Task<HashSet<T>> AddAsync(T item);
        Task<HashSet<T>> UpdateAsync(T item, Guid id);
        Task<HashSet<T>> RemoveAsync(Guid id);
    }
}