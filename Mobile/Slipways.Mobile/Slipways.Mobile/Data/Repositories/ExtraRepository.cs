using Slipways.Mobile.Contracts;
using Slipways.Mobile.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slipways.Mobile.Data.Repositories
{
    public class ExtraRepository : BaseRepository<Extra>, IExtraRepository
    {
        public ExtraRepository(IDataContext dataContext) : base(dataContext)
        {
        }
    }
}
