using com.b_velop.Slipways.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IPortRepository : IRepositoryBase<Port>
    {
        Task<ILookup<Guid, Port>> GetPortsByWaterIdAsync(IEnumerable<Guid> waterIds, CancellationToken cancellationToken);
        Task<IDictionary<Guid, Port>> GetPortsByIdAsync(IEnumerable<Guid> portIds, CancellationToken cancellationToken);
    }
}