using AutoMapper;
using com.b_velop.Slipways.Data.Dtos;
using com.b_velop.Slipways.Data.Models;
using com.b_velop.Utilities.Extensions;

namespace com.b_velop.Slipways.Data.Helper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Extra, ExtraDto>();
            CreateMap<ExtraDto, Extra>();

            CreateMap<ManufacturerDto, Manufacturer>();
            CreateMap<Manufacturer, ManufacturerDto>();

            CreateMap<Port, PortDto>();
            CreateMap<PortDto, Port>();

            CreateMap<Service, ServiceDto>();
            CreateMap<ServiceDto, Service>();

            CreateMap<StationDto, Station>();
            CreateMap<Station, StationDto>();

            CreateMap<Water, WaterDto>().ReverseMap();
            //CreateMap<WaterDto, Water>();

            CreateMap<Slipway, SlipwayDto>().ReverseMap();
            //CreateMap<SlipwayDto, Slipway>();
            //.ForMember(dest => dest.Water, opt => opt.MapFrom(src => src.Extras));

            CreateMap<Slipway, SlipwayForListDto>()
                .ForMember(source => source.Water, opt => opt.MapFrom(_ => _.Water.Longname.FirstUpper()));
        }
    }
}
