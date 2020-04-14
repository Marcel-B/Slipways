using Newtonsoft.Json;
using Slipways.Mobile.Contracts;
using SQLite;
using System;

namespace Slipways.Mobile.Data.Models
{
    public class Station : IEntity
    {
        [JsonIgnore]
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [JsonProperty("id")]
        public Guid Pk { get; set; }

        public string Agency { get; set; }
        public double Km { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Longname { get; set; }
        public string Number { get; set; }
        public DateTime? Updated { get; set; }

        [JsonIgnore]
        [Ignore]
        public string Name { get => Longname; }

        [JsonIgnore]
        public Guid WaterPk { get; set; }

        [Ignore]
        public Water Water { get; set; }
    }
}
