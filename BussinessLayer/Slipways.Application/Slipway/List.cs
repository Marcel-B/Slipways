using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;

namespace com.b_velop.Slipways.Application.Slipway
{
    public class List
    {
        public class Query : IRequest<IEnumerable<SlipwayDto>> { }
        public class Handler : IRequestHandler<Query, IEnumerable<SlipwayDto>>
        {
            private IMapper _mapper;
            private ISlipwayRepository _repo;

            public Handler(
                IMapper mapper,
                ISlipwayRepository repo)
            {
                _mapper = mapper;
                _repo = repo;
            }

            public async Task<IEnumerable<SlipwayDto>> Handle(
                Query request,
                CancellationToken cancellationToken)
            {
                var slipways = await _repo.GetSlipways(cancellationToken);
                var result = _mapper.Map<IEnumerable<SlipwayDto>>(slipways);
                return result;
            }
        }
    }
}
