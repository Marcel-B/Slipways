using com.b_velop.Slipways.Data.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Data.Repositories
{
    public abstract class RepositoryBase : IRepositoryBase
    {
        protected Persistence.SlipwaysContext Context { get; }

        protected RepositoryBase(Persistence.SlipwaysContext context)
        {
            Context = context;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await Context.SaveChangesAsync(cancellationToken) > 0;
    }
}
