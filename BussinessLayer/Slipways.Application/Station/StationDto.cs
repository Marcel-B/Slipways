using System;

namespace com.b_velop.Slipways.Application.Station
{
    public class StationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Km { get; set; }
        public string Agency { get; set; }
        public string Water { get; set; }
    }
}