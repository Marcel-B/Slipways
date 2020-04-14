using com.b_velop.Slipways.Web.Contracts;

namespace com.b_velop.Slipways.Web.Data
{
    public class StoreWrapper : IStoreWrapper
    {
        public IServiceStore Services { get; }
        public IExtraStore Extras { get; }
        public IManufacturerStore Manufacturers { get; }
        public ISlipwayStore Slipways { get; }
        public IWaterStore Waters { get; }
        public IPortStore Ports { get; }

        public StoreWrapper(
            IServiceStore serviceStore,
            IExtraStore extraStore,
            IManufacturerStore manufacturerStore,
            ISlipwayStore slipwayStore,
            IWaterStore waterStore,
            IPortStore portStore)
        {
            Services = serviceStore;
            Extras = extraStore;
            Manufacturers = manufacturerStore;
            Slipways = slipwayStore;
            Waters = waterStore;
            Ports = portStore;
        }
    }
}
