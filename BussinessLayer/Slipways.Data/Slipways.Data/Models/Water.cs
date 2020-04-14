using System;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class Water : Entity
    {
        [JsonProperty("shortname")]
        public string Shortname { get; set; }

        [JsonProperty("longname")]
        public string Longname { get; set; }
    }
}
