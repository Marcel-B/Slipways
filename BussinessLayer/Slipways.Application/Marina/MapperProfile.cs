using AutoMapper;

namespace com.b_velop.Slipways.Application.Marina
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Domain.Models.Marina, MarinaDto>()  
                .ForMember(m => m.Water, opt => opt.MapFrom(m => m.Water.Name));

        }
    }
}