using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IRepositoryBase
    {
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
