using Slipways.Mobile.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slipways.Mobile.Contracts
{
    public interface IRepositoryWrapper
    {
        ISlipwayRepository Slipways { get; }
        IExtraRepository Extras { get; }
        IManufacturerRepository Manufacturers { get; }
        IMarinaRepository Marinas { get; }
        IServiceRepository Services { get; }
        IStationRepository Stations { get; }
        IWaterRepository Waters { get; }
        ISlipwayExtraRepository SlipwayExtras { get; }
        Task<IEnumerable<T>> GetAllAsync<T>() where T : IEntity, new();
        Task<IEnumerable<Slipway>> GetAllSlipwaysWithSubsets();
    }
}
