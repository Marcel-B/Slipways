using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Domain.Models
{
    public class Station
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Number { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Shortname { get; set; }
        public string Name { get; set; }
        public double Km { get; set; }
        public string Agency { get; set; }

        public Guid WaterId { get; set; }
        public virtual Water Water { get; set; }
    }
}
