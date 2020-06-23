using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Application.Slipway;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;

namespace com.b_velop.Slipways.Application.Station
{
    public class Details
    {
        public class Query : IRequest<StationDetailsDto>
        {
            public Guid Id { get; set; }
        }


        public class Handler : IRequestHandler<Query, StationDetailsDto>
        {
            private readonly IMapper _mapper;
            private readonly IStationRepository _repo;

            public Handler(
                IMapper mapper,
                IStationRepository repo)
            {
                _mapper = mapper;
                _repo = repo;
            }
            
            public async Task<StationDetailsDto> Handle(
                Query request, 
                CancellationToken cancellationToken)
            {
                var id = request.Id;
                var slipway = await _repo.GetStation(id, cancellationToken);
                return _mapper.Map<StationDetailsDto>(slipway);
                //throw new System.NotImplementedException("Sorry, no content");
            }
        }
        
    }
}