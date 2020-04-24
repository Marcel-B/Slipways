using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using com.b_velop.Slipways.Data.Contracts;
using com.b_velop.Slipways.Data.Repositories;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Slipways.GrQl.Data.Models;
using com.b_velop.Slipways.Persistence;
using Microsoft.EntityFrameworkCore;

namespace com.b_velop.Slipways.GrQl.Data
{
    public interface IGraphQlRepository : IRepositoryBase
    {
        Task<ILookup<Guid, ExtraDto>> GetExtrasBySlipways(
            IEnumerable<Guid> slipwayIds,
            CancellationToken cancellationToken = default);

        Task<ILookup<Guid, SlipwayDto>> GetSlipwaysByExtras(
            IEnumerable<Guid> extraIds,
            CancellationToken cancellationToken = default);
        Task<ILookup<Guid, ManufacturerDto>> GetManufacturersByServices(IEnumerable<Guid> serviceIds, CancellationToken cancellationToken = default);
        Task<ILookup<Guid, ServiceDto>> GetServicesByManufacturers(IEnumerable<Guid> serviceIds, CancellationToken cancellationToken = default);
    }

    public class GraphQlRepository : RepositoryBase, IGraphQlRepository
    {
        private readonly IMapper _mapper;

        public GraphQlRepository(
            IMapper mapper,
            SlipwaysContext context) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<ILookup<Guid, SlipwayDto>> GetSlipwaysByExtras(
            IEnumerable<Guid> extraIds,
            CancellationToken cancellationToken = default)
        {
            var slipwaysWithExtra = await Context
                .SlipwayExtras
                .Where(se => extraIds.Contains(se.ExtraId))
                .ToListAsync(cancellationToken);
            
            var result = new List<SlipwayDto>();

            foreach (var slipwayExtra in slipwaysWithExtra)
            {
                var slipway = Context.Slipways.Single(e => e.Id == slipwayExtra.SlipwayId);
                var slipwayDto = _mapper.Map<SlipwayDto>(slipway);
                slipwayDto.ExtraId = slipwayExtra.ExtraId;
                result.Add(slipwayDto);
            }

            var returnValue =
                result.ToLookup(p => p.ExtraId);
            return returnValue;
        }
        public async Task<ILookup<Guid, ExtraDto>> GetExtrasBySlipways(
            IEnumerable<Guid> slipwayIds,
            CancellationToken cancellationToken = default)
        {
            var slipwaysWithExtra = await Context
                .SlipwayExtras
                .Where(se => slipwayIds.Contains(se.SlipwayId))
                .ToListAsync(cancellationToken);

            var result = new List<ExtraDto>();

            foreach (var slipwayExtra in slipwaysWithExtra)
            {
                var extra = Context.Extras.Single(e => e.Id == slipwayExtra.ExtraId);
                var extraDto = _mapper.Map<ExtraDto>(extra);
                extraDto.SlipwayId = slipwayExtra.SlipwayId;
                result.Add(extraDto);
            }

            var returnValue =
                result.ToLookup(p => p.SlipwayId);
            return returnValue;
        }
        
        public async Task<ILookup<Guid, ManufacturerDto>> GetManufacturersByServices(
            IEnumerable<Guid> serviceIds,
            CancellationToken cancellationToken = default)
        {
            var  manufacturerServices = await Context
                .ManufacturerServices
                .Where(ms => serviceIds.Contains(ms.ServiceId))
                .ToListAsync(cancellationToken);

            var result = new List<ManufacturerDto>();
            
            foreach (var manufacturerService in manufacturerServices)
            {
                var manufacturer = Context.Manufacturers.Single(m => m.Id == manufacturerService.ManufacturerId);
                var dto = _mapper.Map<ManufacturerDto>(manufacturer);
                dto.ServiceId = manufacturerService.ServiceId;
                result.Add(dto);
            }

            var returnValue =
                result.ToLookup(p => p.ServiceId);
            return returnValue;
        }
        
        public async Task<ILookup<Guid, ServiceDto>> GetServicesByManufacturers(
            IEnumerable<Guid> manufacturerIds,
            CancellationToken cancellationToken = default)
        {
            var manufacturerServices = await Context
                .ManufacturerServices
                .Where(se => manufacturerIds.Contains(se.ManufacturerId))
                .ToListAsync(cancellationToken);

            var result = new List<ServiceDto>();
            
            foreach (var manufacturerService in manufacturerServices)
            {
                var service = Context.Services.Single(s => s.Id == manufacturerService.ServiceId);
                var dto = _mapper.Map<ServiceDto>(service);
                dto.ManufacturerId = manufacturerService.ManufacturerId;
                result.Add(dto);
            }

            var returnValue =
                result.ToLookup(p => p.ManufacturerId);
            return returnValue;
        }
    }
}
