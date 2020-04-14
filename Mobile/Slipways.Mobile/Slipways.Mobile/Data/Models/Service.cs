using Newtonsoft.Json;
using Slipways.Mobile.Contracts;
using SQLite;
using System;
using System.Collections.Generic;

namespace Slipways.Mobile.Data.Models
{
    public class Service : IEntity
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty("id")]
        public Guid Pk { get; set; }

        public string Name { get; set; }
        public string Street { get; set; }
        public string Postalcode { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public DateTime? Updated { get; set; }

        [Ignore]
        public IEnumerable<Manufacturer> Manufacturers { get; set; }
    }
}
