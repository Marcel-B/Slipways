using System;
using AutoMapper;

namespace com.b_velop.Slipways.Application.Slipway
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Domain.Models.Slipway, SlipwayDto>();
        }
    }
}
