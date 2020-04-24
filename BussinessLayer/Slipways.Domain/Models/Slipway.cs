using System;
using System.Collections.Generic;

namespace com.b_velop.Slipways.Domain.Models
{
    public class Slipway 
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Name { get; set; }

        public int Rating { get; set; }
        public string Comment { get; set; }
        public string Street { get; set; }
        public string Postalcode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public decimal Costs { get; set; }
        public string Pro { get; set; }
        public string Contra { get; set; }

        public Guid? MarinaId { get; set; }
        public virtual Marina Marina { get; set; }
        public Guid WaterId { get; set; }
        public virtual Water Water { get; set; }
        public virtual ICollection<SlipwayExtra> SlipwayExtras { get; set; }
    }
}
