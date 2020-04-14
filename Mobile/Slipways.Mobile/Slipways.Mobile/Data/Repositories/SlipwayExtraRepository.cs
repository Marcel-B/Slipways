using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;

namespace Slipways.Mobile.Data.Repositories
{
    public class SlipwayExtraRepository : BaseRepository<SlipwayExtra>, ISlipwayExtraRepository
    {
        public SlipwayExtraRepository(
            IDataContext context) : base(context)
        {
        }

        public async override Task<List<SlipwayExtra>> GetAllAsync()
        {
            return await base.GetAllAsync();
        }
        public async Task<SlipwayExtra> GetBySlipwayPkAsync(
            Guid pk)
        {
            var slipwayExtra = await Context.Table<SlipwayExtra>().ToListAsync();
            return slipwayExtra.FirstOrDefault(_ => _.SlipwayPk == pk);
        }

    }
}
