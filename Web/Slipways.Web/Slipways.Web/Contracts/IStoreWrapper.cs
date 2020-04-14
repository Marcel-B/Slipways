namespace com.b_velop.Slipways.Web.Contracts
{
    public interface IStoreWrapper
    {
        IServiceStore Services { get; }
        IExtraStore Extras { get; }
        IManufacturerStore Manufacturers { get; }
        ISlipwayStore Slipways { get; }
        IWaterStore Waters { get; }
        IPortStore Ports { get; }
    }
}
