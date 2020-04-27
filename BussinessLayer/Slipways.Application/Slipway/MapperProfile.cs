using System;
using AutoMapper;

namespace com.b_velop.Slipways.Application.Slipway
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Domain.Models.Slipway, SlipwayDto>()
                .ForMember(s => s.Water, opt => opt.MapFrom(s => s.Water.Name));
            CreateMap<Domain.Models.Slipway, SlipwayDetailsDto>()
                .ForMember(s => s.Water, opt => opt.MapFrom(s => s.Water.Name));
        }
    }
}
