using com.b_velop.Slipways.Data.Models;
using System.Linq;

namespace com.b_velop.Slipways.Data.Extensions
{
    public static class ServiceWrapper
    {
        public static Service Copy(
            this Service s)
        {
            var service = new Service
            {
                Id = s.Id,
                City = s.City,
                Created = s.Created,
                Email = s.Email,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                ManufacturerFk = s.ManufacturerFk,
                Manufacturers = s.Manufacturers?.Select(_ => _.Copy())?.ToList(),
                Name = s.Name,
                Phone = s.Phone,
                Postalcode = s.Postalcode,
                Street = s.Street,
                Updated = s.Updated,
                Url = s.Url
            };
            return service;
        }
    }
}
