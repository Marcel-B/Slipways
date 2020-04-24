using com.b_velop.Slipways.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IExtraRepository  : IRepositoryBase
    {
        Task<IEnumerable<Extra>> GetExtras(CancellationToken cancellationToken = default);
    }
}
