using System;
using com.b_velop.Slipways.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class ServiceRepository : RepositoryBase, IServiceRepository
    {
        public ServiceRepository(
           Persistence.SlipwaysContext context)  : base(context)
        {
        }

        public async Task<IEnumerable<Service>> GetServices(
            CancellationToken cancellationToken = default)
            => await Context.Services.ToListAsync(cancellationToken);
    }
}
