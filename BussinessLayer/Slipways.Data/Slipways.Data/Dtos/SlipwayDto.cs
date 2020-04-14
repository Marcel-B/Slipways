using System;
using System.Collections.Generic;
using com.b_velop.Slipways.Data.Contracts;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Dtos
{
    public class SlipwayDto : IDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("updated")]
        public DateTime? Updated { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("waterFk")]
        public Guid WaterFk { get; set; }

        [JsonProperty("portFk")]
        public Guid PortFk { get; set; }

        [JsonProperty("rating")]
        public int Rating { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("postalcode")]
        public string Postalcode { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("costs")]
        public decimal Costs { get; set; }

        [JsonProperty("pro")]
        public string Pro { get; set; }

        [JsonProperty("contra")]
        public string Contra { get; set; }

        [JsonProperty("extras")]
        public IEnumerable<ExtraDto> Extras { get; set; }

        [JsonProperty("water")]
        public WaterDto Water { get; set; }

        [JsonProperty("port")]
        public PortDto Port { get; set; }
    }
}