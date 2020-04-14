using System;
using Newtonsoft.Json;
using Slipways.Mobile.Contracts;
using SQLite;

namespace Slipways.Mobile.Data.Models
{
    public class Manufacturer : IEntity
    {
        [JsonIgnore]
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime? Updated { get; set; }

        [JsonProperty("id")]
        [Indexed]
        public Guid Pk { get; set; }
    }
}
