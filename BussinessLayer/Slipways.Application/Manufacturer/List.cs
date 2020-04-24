using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;

namespace com.b_velop.Slipways.Application.Manufacturer
{
    public class List
    {
        public class Query : IRequest<IEnumerable<ManufacturerDto>>{   }

        public class Handler : IRequestHandler<Query, IEnumerable<ManufacturerDto>>
        {
            private readonly IMapper _mapper;
            private readonly IManufacturerRepository _repo;

            public Handler(
                IMapper mapper,
                IManufacturerRepository repo)
            {
                _mapper = mapper;
                _repo = repo;
            }

            public async Task<IEnumerable<ManufacturerDto>> Handle(
                Query request,
                CancellationToken cancellationToken)
            {
                var manufacturers = await _repo.GetManufacturers(cancellationToken);
                return _mapper.Map<IEnumerable<ManufacturerDto>>(manufacturers);
            }
        }
    }
}