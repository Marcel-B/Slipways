using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;

namespace Slipways.Mobile.Data.Repositories
{
    public class MarinaRepository : BaseRepository<Marina>, IMarinaRepository
    {
        public MarinaRepository(
            IDataContext dataContext) : base(dataContext) { }

        public override async Task<List<Marina>> GetAllAsync()
        {
            if (Cache != null && Cache.Count > 0)
                return Cache;

            var marinas = await base.GetAllAsync();
            var waters = await Context.Table<Water>().ToListAsync();
            foreach (var marina in marinas)
            {
                var water = waters.First(_ => _.Pk == marina.WaterPk);
                marina.Water = water;
            }
            Cache = marinas;
            return Cache;
        }
    }
}
