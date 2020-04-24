using System;
using com.b_velop.Slipways.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface ISlipwayRepository  : IRepositoryBase
    {
        Task<IEnumerable<Slipway>> GetSlipways(CancellationToken cancellationToken = default);
        Task<Slipway> GetSlipway(Guid id, CancellationToken cancellationToken = default);
        Task<Slipway> GetSlipwayOrDefault(Guid id, CancellationToken cancellationToken = default);
        Task<ILookup<Guid, Slipway>> GetSlipwaysByMarinas(IEnumerable<Guid> slipwaysIds,CancellationToken cancellationToken = default);
        Task<ILookup<Guid, Slipway>> GetSlipwaysByWaters(IEnumerable<Guid> slipwaysIds, CancellationToken cancellationToken = default);
    }
}