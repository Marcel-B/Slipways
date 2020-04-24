using System;

namespace com.b_velop.Slipways.Application.Manufacturer
{
    public class ManufacturerDto
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Name { get; set; }
    }
}