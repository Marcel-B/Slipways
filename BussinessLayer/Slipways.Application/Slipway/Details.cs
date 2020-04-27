using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace com.b_velop.Slipways.Application.Slipway
{
    public class Details
    {
        public class Query : IRequest<SlipwayDetailsDto>
        {
            public Guid Id { get; set; }
        }


        public class Handler : IRequestHandler<Query, SlipwayDetailsDto>
        {
            private readonly IMapper _mapper;
            private readonly ISlipwayRepository _repo;

            public Handler(
                IMapper mapper,
                ISlipwayRepository repo)
            {
                _mapper = mapper;
                _repo = repo;
            }
            
            public async Task<SlipwayDetailsDto> Handle(
                Query request, 
                CancellationToken cancellationToken)
            {
                var id = request.Id;
                var slipway = await _repo.GetSlipway(id, cancellationToken);
                return _mapper.Map<SlipwayDetailsDto>(slipway);
                //throw new System.NotImplementedException("Sorry, no content");
            }
        }
        
    }
}