using System;
using com.b_velop.Slipways.Data.Contracts;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Dtos
{
    public class ManufacturerDto : IDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
