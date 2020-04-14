using System;
using System.Threading.Tasks;
using Slipways.Mobile.Data.Models;

namespace Slipways.Mobile.Contracts
{
    public interface ISlipwayExtraRepository : IBaseRepository<SlipwayExtra>
    {
        Task<SlipwayExtra> GetBySlipwayPkAsync(Guid pk);
    }
}
