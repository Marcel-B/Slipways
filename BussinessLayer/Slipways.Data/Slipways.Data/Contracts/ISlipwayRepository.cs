using com.b_velop.Slipways.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface ISlipwayRepository : IRepositoryBase<Slipway>
    {
        Task<ILookup<Guid, Slipway>> GetSlipwaysByPortIdAsync(IEnumerable<Guid> portIds, CancellationToken cancellationToken);
        Task<ILookup<Guid, Slipway>> GetSlipwayByWaterIdAsync(IEnumerable<Guid> waterIds, CancellationToken cancellationToken);
        Task<ILookup<Guid, Slipway>> GetSlipwaysByExtraIdAsync(IEnumerable<Guid> extraIds, CancellationToken cancellationToken);
        Task<Slipway> AddPortToSlipwayAsync(Guid slipwayId, Guid portId);
    }
}