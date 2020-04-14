using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;

namespace Slipways.Mobile.Data.Repositories
{
    public class WaterRepository : BaseRepository<Water>, IWaterRepository
    {
        public WaterRepository(IDataContext dataContext) : base(dataContext) { }
    }
}
