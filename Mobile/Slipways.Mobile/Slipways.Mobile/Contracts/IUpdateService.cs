using System.Threading.Tasks;

namespace Slipways.Mobile.Contracts
{
    public interface IUpdateService
    {
        Task UpdateExtra();
        Task UpdateManufacturer();
        Task UpdateWater();
        Task UpdateSlipway();
        Task UpdateMarina();
        Task UpdateStation();
        void Update(bool start);
    }
}
