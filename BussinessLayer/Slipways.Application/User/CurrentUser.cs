using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Application.Interfaces;
using com.b_velop.Slipways.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace com.b_velop.Slipways.Application.User
{
    public class CurrentUser
    {
        public class Query : IRequest<UserDto>{}

        public class Handler : IRequestHandler<Query, UserDto>
        {
            private readonly IMapper _mapper;
            private readonly UserManager<AppUser> _userManager;
            private readonly IJwtGenerator _jwtGenerator;
            private readonly IUserAccessor _userAccessor;

            public Handler(
                IMapper mapper,
                UserManager<AppUser> userManager,
                IJwtGenerator jwtGenerator, 
                IUserAccessor userAccessor)
            {
                _mapper = mapper;
                _userManager = userManager;
                _jwtGenerator = jwtGenerator;
                _userAccessor = userAccessor;
            }
            
            public async Task<UserDto> Handle(
                Query request,
                CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByNameAsync(_userAccessor.GetCurrentUsername());
                user.Token = _jwtGenerator.CreateToken(user);
                return _mapper.Map<UserDto>(user);
            }
        }
    }
}