using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using com.b_velop.Slipways.Domain.Identity;
using com.b_velop.Slipways.Domain.Models;
using com.b_velop.Utilities.Extensions;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Persistence
{
    public class Seed
    {
        private class ServiceSeed
        {
            public Guid Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Updated { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string Street { get; set; }
            public string Postalcode { get; set; }
            public string City { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
        }

        private class ManufacturerServiceSeed
        {
           public Guid ServiceFk { get; set; }
           public Guid ManufacturerFk { get; set; }
        }

        private class SlipwayExtraSeed
        {
            public Guid Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Updated { get; set; }
            public Guid  SlipwayFk { get; set; }
            public Guid  ExtraFk { get; set; }
        }
        private class SlipwaySeed
        {
            public Guid Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Updated { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Name { get; set; }
            public Guid WaterFk { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public string Street { get; set; }
            public string Postalcode { get; set; }
            public string City { get; set; }
            public decimal Costs { get; set; }
            public string Pro { get; set; }
            public string Contra { get; set; }
            public Guid? PortFk { get; set; }
            public string Country { get; set; }
        }

        private class StationSeed
        {
            public Guid Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Updated { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Number { get; set; }
            public string Shortname { get; set; }
            public string Longname { get; set; }
            public double Km { get; set; }
            public string Agency { get; set; }
            public Guid WaterFk { get; set; }
        }

        private class ExtraSeed
        {
            public Guid Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Updated { get; set; }
            public string Name { get; set; }
        }

        private class MarinaSeed
        {
            public Guid Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Updated { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string Street { get; set; }
            public string Postalcode { get; set; }
            public string City { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public Guid WaterFk { get; set; }
        }

        private class WaterSeed
        {
            public Guid Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Updated { get; set; }
            public string Shortname { get; set; }
            public string Longname { get; set; }
        }

        public static async Task SeedData(
            SlipwaysContext context,
            UserManager<AppUser> userManager)
        {
            var manufacturers = new[]
            {
                new Manufacturer
                {
                    Id = Guid.Parse("0FD675D0-867D-41EA-B14F-D1B2C264DAC7"),
                    Created = DateTime.Now,
                    Name = "Quicksilver"
                },new Manufacturer
                {
                    Id = Guid.Parse("102A6D4B-BD31-4428-BC16-E6991A1A9A1D"),
                    Created = DateTime.Now,
                    Name = "Johnson"
                },
                new Manufacturer
                {
                    Id = Guid.Parse("8BAA7BF4-CB1D-4D5E-B3C8-EF190DAC1C3C"),
                    Created = DateTime.Now,
                    Name = "Suzuki"
                },
                 new Manufacturer
                {
                    Id = Guid.Parse("45A1882C-4822-4CD1-9621-CEBDA51BFA05"),
                    Created = DateTime.Now,
                    Name = "Mercury"
                },
                  new Manufacturer
                {
                    Id = Guid.Parse("E2931187-E433-4A36-8A0A-93900564A741"),
                    Created = DateTime.Now,
                    Name = "Volvo Penta"
                },
                   new Manufacturer
                {
                    Id = Guid.Parse("9F36C0C2-F1EE-46FE-BEF0-77F6E913BF41"),
                    Created = DateTime.Now,
                    Name = "Tohatsu"
                },
                    new Manufacturer
                {
                    Id = Guid.Parse("75EE4DD3-5933-4904-9020-5836EC757F02"),
                    Created = DateTime.Now,
                    Name = "Honda"
                },
                     new Manufacturer
                {
                    Id = Guid.Parse("7A73006A-6825-43A3-A894-57BD588529B3"),
                    Created = DateTime.Now,
                    Name = "Verado"
                },
                      new Manufacturer
                {
                    Id = Guid.Parse("D07683A9-2FFA-4763-A047-31214E8EB9C9"),
                    Created = DateTime.Now,
                    Name = "Yamaha"
                }
            };

            foreach (var manufacturer in manufacturers)
            {
                var tmp = context.Manufacturers.Find(manufacturer.Id);
                    if (tmp == null)
                        context.Manufacturers.Add(manufacturer);
            }

            var p = Path.Combine(AppContext.BaseDirectory, "waters.json");
            var w = File.ReadAllText(p);
            var waters = JsonConvert.DeserializeObject<IEnumerable<WaterSeed>>(w);
            foreach (var water in waters)
            {
                var tmp = context.Waters.Find(water.Id);
                if (tmp == null)
                {
                    context.Waters.Add(new Water
                    {
                        Id = water.Id,
                        Created = water.Created,
                        Name = water.Longname.FirstUpper(),
                        Shortname = water.Shortname,
                        Updated = water.Updated
                    });
                }
            }
            p = Path.Combine(AppContext.BaseDirectory, "marinas.json");
            w = File.ReadAllText(p);
            var marinas = JsonConvert.DeserializeObject<IEnumerable<MarinaSeed>>(w);
            foreach (var marina in marinas)
            {
                var tmp = context.Marinas.Find(marina.Id);
                if (tmp == null)
                {
                    context.Marinas.Add(new Marina
                    {
                        Id = marina.Id,
                        Created = marina.Created,
                        Name = marina.Name,
                        City = marina.City,
                        Email = marina.Email,
                        Latitude = marina.Latitude,
                        Longitude = marina.Longitude,
                        Phone = marina.Phone,
                        Postalcode = marina.Postalcode,
                        Street = marina.Street,
                        Url = marina.Url,
                        WaterId = context.Waters.Find(marina.WaterFk)?.Id ?? Guid.Empty,
                        Updated = marina.Updated
                    });
                }
            }

            p = Path.Combine(AppContext.BaseDirectory, "extras.json");
            w = File.ReadAllText(p);
            var extras = JsonConvert.DeserializeObject<IEnumerable<ExtraSeed>>(w);
            foreach (var extra in extras)
            {
                var tmp = context.Extras.Find(extra.Id);
                if (tmp == null)
                {
                    context.Extras.Add(new Extra
                    {
                        Id = extra.Id,
                        Created = extra.Created,
                        Updated = extra.Updated,
                        Name = extra.Name
                    });
                }
            }

            p = Path.Combine(AppContext.BaseDirectory, "slipways.json");
            w = File.ReadAllText(p);
            var slipways = JsonConvert.DeserializeObject<IEnumerable<SlipwaySeed>>(w);
            foreach (var slipway in slipways)
            {
                var tmp = context.Slipways.Find(slipway.Id);
                if (tmp == null)
                {
                    context.Slipways.Add(new Slipway
                    {
                        Id = slipway.Id,
                        City = slipway.City,
                        Comment = slipway.Comment,
                        Contra = slipway.Contra,
                        Costs = slipway.Costs,
                        Country = slipway.Country,
                        Created = slipway.Created,
                        Latitude = slipway.Latitude,
                        Longitude = slipway.Longitude,
                        Name = slipway.Name,
                        Postalcode = slipway.Postalcode,
                        Pro = slipway.Pro,
                        Rating =  slipway.Rating,
                        Street = slipway.Street,
                        WaterId = slipway.WaterFk,
                        MarinaId = slipway.PortFk,
                        Updated = slipway.Updated,
                    });
                }
            }

            p = Path.Combine(AppContext.BaseDirectory, "slipwayExtra.json");
            w = File.ReadAllText(p);
            var slipwayExtras = JsonConvert.DeserializeObject<IEnumerable<SlipwayExtraSeed>>(w);
            foreach (var slipwayExtra in slipwayExtras)
            {
                var tmp = context.SlipwayExtras.Find(slipwayExtra.SlipwayFk, slipwayExtra.ExtraFk);
                if (tmp == null)
                {
                    context.SlipwayExtras.Add(new SlipwayExtra
                    {
                        SlipwayId = slipwayExtra.SlipwayFk,
                        ExtraId = slipwayExtra.ExtraFk
                    });
                }
            }

            p = Path.Combine(AppContext.BaseDirectory, "services.json");
            w = File.ReadAllText(p);
            var services = JsonConvert.DeserializeObject<IEnumerable<ServiceSeed>>(w);
            foreach (var service in services)
            {
                var tmp = context.Services.Find(service.Id);
                if (tmp == null)
                {
                    context.Services.Add(new Service
                    {
                        Id = service.Id,
                        Created =  service.Created,
                        Updated = service.Updated,
                        Name = service.Name,
                        City = service.City,
                        Email = service.Email,
                        Latitude = service.Latitude,
                        Longitude = service.Longitude,
                        Phone = service.Phone,
                        Postalcode = service.Postalcode,
                        Street = service.Street,
                        Url = service.Url,
                    });
                }
            }

            p = Path.Combine(AppContext.BaseDirectory, "manufacturerService.json");
            w = File.ReadAllText(p);
            var manufacturerServices = JsonConvert.DeserializeObject<IEnumerable<ManufacturerServiceSeed>>(w);
            foreach (var manufacturerService in manufacturerServices)
            {
                var tmp = context.ManufacturerServices.Find(manufacturerService.ManufacturerFk, manufacturerService.ServiceFk);
                if (tmp == null)
                    context.ManufacturerServices.Add(new ManufacturerService
                    {
                        ManufacturerId = manufacturerService.ManufacturerFk,
                        ServiceId = manufacturerService.ServiceFk
                    });
            }

            p = Path.Combine(AppContext.BaseDirectory, "stations.json");
            w = File.ReadAllText(p);
            var stations = JsonConvert.DeserializeObject<IEnumerable<StationSeed>>(w);
            foreach (var station in stations)
            {
                var tmp = context.Stations.Find(station.Id);
                if (tmp != null) continue;
                //var name = station.Longname != null ? station.Longname.FirstUpper() : "";
                context.Stations.Add(new Station()
                {
                    Id = station.Id,
                    Agency =  station.Agency,
                    Km =  station.Km,
                    Latitude = station.Latitude,
                    Longitude = station.Longitude,
                    Shortname = station.Shortname,
                    Name =  station.Longname,
                    Number = station.Number,
                    WaterId = station.WaterFk,
                    Created = station.Created,
                    Updated = station.Updated
                });
            }

            context.SaveChanges();
        }
    }
}
