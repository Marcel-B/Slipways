using Newtonsoft.Json;
using Slipways.Mobile.Contracts;
using SQLite;
using System;
namespace Slipways.Mobile.Data.Models
{
    public class Marina : IEntity
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

        [Ignore]
        public Water Water { get; set; }

        public DateTime? Updated { get; set; }

        public string Watername => Water?.Name;

        [JsonIgnore]
        public Guid WaterPk { get; set; }
    }
}
