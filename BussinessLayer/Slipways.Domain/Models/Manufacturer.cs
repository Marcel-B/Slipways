using System;
using System.Collections.Generic;

namespace com.b_velop.Slipways.Domain.Models
{
    public class Manufacturer
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Name { get; set; }

        public virtual ICollection<ManufacturerService> ManufacturerServices { get; set; }
    }
}
