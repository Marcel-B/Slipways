using System;

namespace com.b_velop.Slipways.Domain.Models
{
    public class Water
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Name { get; set; }
        public string Shortname { get; set; }
    }
}
