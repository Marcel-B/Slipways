using AutoMapper;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.GrQl.Data.Models
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Slipway, SlipwayDto>();
            CreateMap<Extra, ExtraDto>();
            CreateMap<Service, ServiceDto>();
            CreateMap<Manufacturer, ManufacturerDto>();
        }
    }
}