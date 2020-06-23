using AutoMapper;

namespace com.b_velop.Slipways.Application.Station
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Domain.Models.Station, StationDto>()
             .ForMember(s => s.Water, opt => opt.MapFrom(s => s.Water.Name));

            CreateMap<Domain.Models.Station, StationDetailsDto>()
            .ForMember(s => s.Water, opt => opt.MapFrom(s => s.Water.Name));
        }
    }
}