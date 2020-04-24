using System;
using com.b_velop.Slipways.Data.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class MarinaRepository : RepositoryBase, IMarinaRepository
    {
        public MarinaRepository(
           Persistence.SlipwaysContext context) : base(context)
        { 
        }

        public async Task<IEnumerable<Marina>> GetMarinas(
            CancellationToken cancellationToken = default)
            => await Context.Marinas.ToListAsync(cancellationToken);

        public async Task<IDictionary<Guid, Marina>> GetMarinaDictById(
            IEnumerable<Guid> marinaIds, 
            CancellationToken cancellationToken = default)
        {
            var marinas = await Context.Marinas.Where(m => marinaIds.Contains(m.Id)).ToListAsync(cancellationToken);
            return marinas.ToDictionary(m => m.Id);
        }

        public Task<ILookup<Guid, Marina>> GetMarinasByWaters(
            IEnumerable<Guid> marinaIds, 
            CancellationToken cancellationToken = default)
        {
            var result = Context.Marinas.Where(m => marinaIds.Contains(m.Id)).ToLookup(m => m.WaterId);
            return Task.FromResult(result);
        }
    }
}
