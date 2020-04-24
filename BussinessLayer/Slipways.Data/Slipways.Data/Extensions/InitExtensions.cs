using Microsoft.Extensions.DependencyInjection;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace com.b_velop.Slipways.Data.Extensions
{
    public static class InitExtensions
    {
        public static IServiceCollection AddSlipwaysData(
            this IServiceCollection services)
        {
            services.AddScoped<IExtraRepository, ExtraRepository>();
            services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
            services.AddScoped<IMarinaRepository, MarinaRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<ISlipwayRepository, SlipwayRepository>();
            services.AddScoped<IStationRepository, StationRepository>();
            services.AddScoped<IWaterRepository, WaterRepository>();
            return services;
        }
    }
}
