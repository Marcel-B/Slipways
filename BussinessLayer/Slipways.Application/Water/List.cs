using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;

namespace com.b_velop.Slipways.Application.Water
{
    public class List
    {
        public class Query : IRequest<List<WaterDto>>{}

        public class Handler : IRequestHandler<Query, List<WaterDto>>
        {
            private readonly IMapper _mapper;
            private readonly IWaterRepository _repository;

            public Handler(
                IMapper mapper,
                IWaterRepository repository)
            {
                _mapper = mapper;
                _repository = repository;
            }
            
            public async Task<List<WaterDto>> Handle(
                Query request,
                CancellationToken cancellationToken)
            {
                var waters = await _repository.GetWaters(cancellationToken);
                var result = _mapper.Map<List<WaterDto>>(waters);
                return result;
            }
        }
    }
}