using AutoMapper;

namespace com.b_velop.Slipways.Application.Water
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Domain.Models.Water, WaterDto>();
        }
    }
}