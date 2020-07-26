using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class WaterRepository : RepositoryBase, IWaterRepository
    {
        public WaterRepository(
            Persistence.SlipwaysContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Water>> GetWaters(
            CancellationToken cancellationToken = default)
            => await Context.Waters.ToListAsync(cancellationToken);

        public async Task<IDictionary<Guid, Water>> GetWaterDictById(
            IEnumerable<Guid> waterIds,
            CancellationToken cancellationToken = default)
        {
            var waters = await Context.Waters.Where(w => waterIds.Contains(w.Id)).ToListAsync(cancellationToken);
            return waters.ToDictionary(_ => _.Id);
        }

        public Water AddWater(Water water)
        {
            var result = Context.Waters.Add(water);
            return result.Entity;
        }

        public async Task<Water> GetWater(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await Context.Waters.FirstOrDefaultAsync(_ => _.Id == id, cancellationToken);
            return result;
        }
    }
}
