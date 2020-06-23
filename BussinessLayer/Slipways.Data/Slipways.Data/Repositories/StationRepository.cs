using System;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class StationRepository : RepositoryBase, IStationRepository
    {
        public StationRepository(
            Persistence.SlipwaysContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Station>> GetStations(
            CancellationToken cancellationToken = default)
            => await Context.Stations.ToListAsync(cancellationToken);

        public async Task<Station> GetStation(Guid stationId, CancellationToken cancellationToken = default) 
            => await Context.Stations.FindAsync( new object[] {stationId}, cancellationToken);

        public async Task<ILookup<Guid, Station>> GetStationsLookupById(
            IEnumerable<Guid> stationIds, 
            CancellationToken cancellationToken = default)
        {
            var stations =  await Context.Stations.Where(s => stationIds.Contains(s.Id)).ToListAsync(cancellationToken);
            return stations.ToLookup(x => x.WaterId);
        }
    }
}
