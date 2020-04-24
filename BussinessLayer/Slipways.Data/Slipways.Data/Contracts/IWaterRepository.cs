using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IWaterRepository : IRepositoryBase
    {
        Task<IEnumerable<Water>> GetWaters(CancellationToken cancellationToken = default);
        Task<IDictionary<Guid, Water>> GetWaterDictById(IEnumerable<Guid> waterIds, CancellationToken cancellationToken = default);
    }
}