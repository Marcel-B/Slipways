using com.b_velop.Slipways.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IStationRepository : IRepositoryBase<Station>
    {
        Task<ILookup<Guid, Station>> GetStationsByWaterIdAsync(IEnumerable<Guid> waterIds, CancellationToken cancellationToken);
    }
}
