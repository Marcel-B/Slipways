using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Application.Errors;
using com.b_velop.Slipways.Application.Interfaces;
using com.b_velop.Slipways.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace com.b_velop.Slipways.Application.User
{
    public class Login 
    {
        public class Query : IRequest<UserDto>
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
        
        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.Password).NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Query, UserDto>
        {
            private readonly IMapper _mapper;
            private readonly UserManager<AppUser> _userManager;
            private readonly SignInManager<AppUser> _signInManager;
            private readonly IJwtGenerator _jwtGenerator;

            public Handler(
                IMapper mapper,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager,
                IJwtGenerator jwtGenerator)
            {
                _mapper = mapper;
                _userManager = userManager;
                _signInManager = signInManager;
                _jwtGenerator = jwtGenerator;
            }
            public async Task<UserDto> Handle(
                Query request, 
                CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                    throw new RestException(HttpStatusCode.Unauthorized);
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (result.Succeeded)
                {
                    user.Token = _jwtGenerator.CreateToken(user);
                    return _mapper.Map<UserDto>(user);
                }
                throw new RestException(HttpStatusCode.Unauthorized);
            }
        }
    }
}