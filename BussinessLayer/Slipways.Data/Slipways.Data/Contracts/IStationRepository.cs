using System;
using com.b_velop.Slipways.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IStationRepository : IRepositoryBase
    {
        Task<IEnumerable<Station>> GetStations(CancellationToken cancellationToken = default);
        Task<ILookup<Guid, Station>> GetStationsLookupById(IEnumerable<Guid> stationIds, CancellationToken cancellationToken = default);
    }
}
