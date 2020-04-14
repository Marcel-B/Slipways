using System;
using com.b_velop.Slipways.Data.Contracts;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Dtos
{
    public class WaterDto : IDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("shortname")]
        public string Shortname { get; set; }

        public string Name { get; set; }
    }
}