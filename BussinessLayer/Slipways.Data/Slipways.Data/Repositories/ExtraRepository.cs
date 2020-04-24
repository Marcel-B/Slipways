using System;
using com.b_velop.Slipways.Data.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using com.b_velop.Slipways.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace com.b_velop.Slipways.Data.Repositories
{
    public class ExtraRepository : RepositoryBase, IExtraRepository
    {
        public ExtraRepository(
            Persistence.SlipwaysContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Extra>> GetExtras(
            CancellationToken cancellationToken = default)
            => await Context.Extras.ToListAsync(cancellationToken);
    }
}
