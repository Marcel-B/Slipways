namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IRepositoryWrapper
    {
        IWaterRepository Water { get; }
        IStationRepository Station { get; }
        ISlipwayRepository Slipway { get; }
        IExtraRepository Extra { get; }
        IServiceRepository Service { get; }
        IManufacturerRepository Manufacturer { get; }
        IPortRepository Port { get; }
        ISlipwayExtraRepository SlipwayExtra { get; }
        IManufacturerServicesRepository ManufacturerServices { get; }
        SlipwaysContext Context { get; }
        void SaveChanges();
    }
}
