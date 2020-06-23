using System;

namespace com.b_velop.Slipways.Application.Station
{
    public class StationDetailsDto
    {
        public Guid Id { get; set; }
        public string Number { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Shortname { get; set; }
        public string Name { get; set; }
        public double Km { get; set; }
        public string Agency { get; set; }
        public string Water { get; set; }
    }
}