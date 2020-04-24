using System;
using System.Collections.Generic;

namespace com.b_velop.Slipways.Domain.Models
{
    public class Marina
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Url { get; set; }
        public string Street { get; set; }
        public string Postalcode { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public virtual ICollection<Slipway> Slipways { get; set; }
        public virtual Water Water { get; set; }
        public Guid WaterId { get; set; }
    }
}
