using System;
using com.b_velop.Slipways.Data.Contracts;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Dtos
{
    public class StationDto : IDto
    {
        [JsonProperty("uuid")]
        public Guid Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("shortname")]
        public string Shortname { get; set; }

        [JsonProperty("longname")]
        public string Longname { get; set; }

        [JsonProperty("km")]
        public double Km { get; set; }

        [JsonProperty("agency")]
        public string Agency { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("water")]
        public WaterDto Water { get; set; }
    }
}
