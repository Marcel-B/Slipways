using Newtonsoft.Json;
using Slipways.Mobile.Contracts;
using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Slipways.Mobile.Data.Models
{
    public class Slipway : IEntity
    {
        [JsonIgnore]
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        [JsonProperty("id")]
        [Indexed]
        public Guid Pk { get; set; }

        [Ignore]
        public IEnumerable<Extra> Extras { get; set; }

        public string City { get; set; }

        public string Street { get; set; }

        public string Postalcode { get; set; }

        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public decimal Costs { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [JsonIgnore]
        public Guid WaterPk { get; set; }

        [Ignore]
        public Water Water { get; set; }

        [Ignore]
        [JsonProperty("port")]
        public Marina Marina { get; set; }

        [JsonIgnore]
        public int WaterId { get => Water.Id; }

        [JsonIgnore]
        public bool IsFavorite { get; set; }

        public string Watername => Water?.Name;
        public Guid MarinaPk { get; set; }
    }
}
