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
    public class SlipwayRepository : RepositoryBase, ISlipwayRepository
    {
        public SlipwayRepository(
            Persistence.SlipwaysContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Slipway>> GetSlipways(
            CancellationToken cancellationToken = default)
            => await Context.Slipways.ToListAsync(cancellationToken: cancellationToken);

        public async Task<Slipway> GetSlipwayOrDefault(
            Guid id,
            CancellationToken cancellationToken = default)
            => await Context.Slipways.FirstOrDefaultAsync(_ => _.Id == id, cancellationToken);

  

        public Task<ILookup<Guid, Slipway>> GetSlipwaysByMarinas(IEnumerable<Guid> slipwaysIds, CancellationToken cancellationToken = default)
        {
            var slipways = Context.Slipways.Where(s => slipwaysIds.Contains(s.Id)).ToLookup(s => s.MarinaId ?? Guid.Empty);
            return Task.FromResult(slipways);
        }

        public Task<ILookup<Guid, Slipway>> GetSlipwaysByWaters(
            IEnumerable<Guid> slipwaysIds, 
            CancellationToken cancellationToken = default)
        {
            var slipways = Context.Slipways.Where(s => slipwaysIds.Contains(s.Id)).ToLookup(s => s.WaterId);
            return Task.FromResult(slipways);
        }

        public async Task<Slipway> GetSlipway(
            Guid id,
            CancellationToken cancellationToken = default)
            => await Context.Slipways.FindAsync(new object[] {id}, cancellationToken: cancellationToken);
    }
}
