using System;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Dtos
{
    public class CurrentMeasurementDto
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("trend")]
        public int Trend { get; set; }

        [JsonProperty("stateMnwMhw")]
        public string StateMnwMhw { get; set; }

        [JsonProperty("stateNswHsw")]
        public string StateNswHsw { get; set; }
    }
}
