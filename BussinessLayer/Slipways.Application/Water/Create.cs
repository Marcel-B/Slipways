using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using FluentValidation;
using MediatR;

namespace com.b_velop.Slipways.Application.Water
{
    public class Create
    {
        public class Command : IRequest<WaterDto>
        {
            public string Name { get; set; }
            public string Shortname { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Name).NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Command, WaterDto>
        {
            private readonly IWaterRepository _repository;
            private readonly IMapper _mapper;

            public Handler(IWaterRepository repository, IMapper mapper)
            {
                _repository = repository;
                _mapper = mapper;
            }

            public async Task<WaterDto> Handle(Command request, CancellationToken cancellationToken)
            {
                var water = new Domain.Models.Water();
                water.Created = DateTime.UtcNow;
                water.Name = request.Name;
                water.Shortname = request.Shortname ?? water.Name;
                water = _repository.AddWater(water);
                _ = await _repository.SaveChangesAsync();
                return _mapper.Map<WaterDto>(water);
            }
        }
    }
}
