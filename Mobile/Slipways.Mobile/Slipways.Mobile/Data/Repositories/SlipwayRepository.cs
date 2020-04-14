using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slipways.Mobile.Data.Repositories
{
    public class SlipwayRepository : BaseRepository<Slipway>, ISlipwayRepository
    {
        public SlipwayRepository(
            IDataContext dataContext) : base(dataContext)
        { }

        public override async Task<List<Slipway>> GetAllAsync()
        {
            if (Cache != null && Cache.Count > 0)
                return Cache;

            var slipways = await base.GetAllAsync();

            var waters = await Context.Table<Water>().ToListAsync();
            var marinas = await Context.Table<Marina>().ToListAsync();
            var extrass = await Context.Table<Extra>().ToListAsync();
            foreach (var slipway in slipways)
            {
                var water = waters.FirstOrDefault(_ => _.Pk == slipway.WaterPk);
                slipway.Water = water;
                var marina = marinas.FirstOrDefault(_ => _.Pk == slipway.MarinaPk);
                var extras = await Context.QueryAsync<SlipwayExtra>($"select * from slipwayextra where slipwaypk = '{slipway.Pk}'");
                var tmpExtras = new List<Extra>();
                foreach (var extra in extras)
                {
                    tmpExtras.Add(extrass.First(_ => _.Pk == extra.ExtraPk));
                }
                slipway.Extras = tmpExtras;

                if (marina != null)
                    slipway.Marina = marina;
            }
            Cache = slipways
                .OrderBy(_ => _.Name)
                .ToList();
            return Cache;
        }
    }
}

