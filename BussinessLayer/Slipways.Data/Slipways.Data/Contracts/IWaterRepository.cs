using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Data.Models;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IWaterRepository : IRepositoryBase<Water>
    {
        Task<IDictionary<Guid, Water>> GetWatersByIdAsync(IEnumerable<Guid> waterIds,
             CancellationToken cancellationToken);
    }
}