using System.Collections.Generic;
using System.Threading.Tasks;
using Slipways.Mobile.Data.Models;

namespace Slipways.Mobile.Contracts
{
    public interface IDataStore
    {
        //IEnumerable<Manufacturer> Manufacturers { get; }
        //IEnumerable<Extra> Extras { get; }
        //IEnumerable<Slipway> Slipways { get; }
        //IEnumerable<Water> Waters { get; }
        //IEnumerable<Marina> Marinas { get; }
        //IEnumerable<Service> Services { get; }

        Task LoadData();
    }
}
