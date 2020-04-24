using AutoMapper;
using com.b_velop.Slipways.Domain.Identity;

namespace com.b_velop.Slipways.Application.User
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<AppUser, UserDto>();
        }
    }
}