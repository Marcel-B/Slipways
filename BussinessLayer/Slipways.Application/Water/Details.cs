using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;

namespace com.b_velop.Slipways.Application.Water
{
    public class Details
    {
        public class Query : IRequest<WaterDto>
        {
            public Guid Id { get; set; }
        }


        public class Handler : IRequestHandler<Query, WaterDto>
        {
            private readonly IMapper _mapper;
            private readonly IWaterRepository _repo;

            public Handler(
                IMapper mapper,
                IWaterRepository repo)
            {
                _mapper = mapper;
                _repo = repo;
            }

            public async Task<WaterDto> Handle(
                Query request,
                CancellationToken cancellationToken)
            {
                var id = request.Id;
                var water = await _repo.GetWater(id, cancellationToken);
                return _mapper.Map<WaterDto>(water);
                //throw new System.NotImplementedException("Sorry, no content");
            }
        }

    }
}
