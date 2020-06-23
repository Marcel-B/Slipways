using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;

namespace com.b_velop.Slipways.Application.Station
{
    public class List
    {
        public class Query : IRequest<IEnumerable<StationDto>> { }
        public class Handler : IRequestHandler<Query, IEnumerable<StationDto>>
        {
            private IMapper _mapper;
            private IStationRepository _repo;

            public Handler(
                IMapper mapper,
                IStationRepository repo)
            {
                _mapper = mapper;
                _repo = repo;
            }

            public async Task<IEnumerable<StationDto>> Handle(
                Query request,
                CancellationToken cancellationToken)
            {
                var stations = await _repo.GetStations(cancellationToken);
                var result = _mapper.Map<IEnumerable<StationDto>>(stations);
                return result;
            }
        }
    }
}