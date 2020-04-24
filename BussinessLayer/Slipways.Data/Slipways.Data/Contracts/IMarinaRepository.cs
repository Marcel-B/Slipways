using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IMarinaRepository  : IRepositoryBase
    {
        Task<IEnumerable<Marina>> GetMarinas(CancellationToken cancellationToken = default);
        Task<IDictionary<Guid, Marina>> GetMarinaDictById(IEnumerable<Guid> marinaIds, CancellationToken cancellationToken = default);
        Task<ILookup<Guid, Marina>> GetMarinasByWaters(IEnumerable<Guid> marinaIds, CancellationToken cancellationToken = default);
    }
}