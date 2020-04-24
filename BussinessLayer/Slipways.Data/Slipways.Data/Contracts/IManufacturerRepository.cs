using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IManufacturerRepository : IRepositoryBase
    {
        Task<IEnumerable<Manufacturer>> GetManufacturers(CancellationToken cancellationToken = default);
    }
}
