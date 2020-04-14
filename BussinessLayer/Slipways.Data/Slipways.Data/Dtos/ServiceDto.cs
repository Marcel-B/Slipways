using System;
using System.Collections.Generic;
using com.b_velop.Slipways.Data.Contracts;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Dtos
{
    public class ServiceDto : IDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("postalcode")]
        public string Postalcode { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("manufacturers")]
        public IList<ManufacturerDto> Manufacturers { get; set; }
    }
}
