using System;

namespace com.b_velop.Slipways.Domain.Models
{
    public class ManufacturerService
    {
        public virtual Manufacturer Manufacturer { get; set; }
        public virtual Service Service { get; set; }

        public Guid ServiceId { get; set; }
        public Guid ManufacturerId { get; set; }
    }
}
