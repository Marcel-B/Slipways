using System;
using com.b_velop.Slipways.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IServiceRepository : IRepositoryBase
    {
        Task<IEnumerable<Service>> GetServices(CancellationToken cancellationToken = default);
    }
}
