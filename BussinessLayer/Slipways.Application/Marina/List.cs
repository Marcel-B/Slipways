using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using MediatR;

namespace com.b_velop.Slipways.Application.Marina
{
    public class List
    {
        public class Query : IRequest<List<MarinaDto>>
        {
            
        }

        public class Hanlder : IRequestHandler<Query, List<MarinaDto>>
        {
            private readonly IMapper _mapper;
            private readonly IMarinaRepository _repo;

            public Hanlder(
                IMapper mapper,
                IMarinaRepository repo)
            {
                _mapper = mapper;
                _repo = repo;
            }
            public async Task<List<MarinaDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var marinas = await _repo.GetMarinas(cancellationToken);
                return _mapper.Map<List<MarinaDto>>(marinas);
            }
        }
    }
}