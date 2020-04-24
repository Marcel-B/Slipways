using AutoMapper;

namespace com.b_velop.Slipways.Application.Manufacturer
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Domain.Models.Manufacturer, ManufacturerDto>();
        }
    }
}