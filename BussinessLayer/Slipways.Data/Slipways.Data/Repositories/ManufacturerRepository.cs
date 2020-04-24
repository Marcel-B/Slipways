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
    public class ManufacturerRepository : RepositoryBase, IManufacturerRepository
    {
        public ManufacturerRepository(
            Persistence.SlipwaysContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Manufacturer>> GetManufacturers(
            CancellationToken cancellationToken = default)
            => await Context.Manufacturers.ToListAsync(cancellationToken: cancellationToken);


    }
}
