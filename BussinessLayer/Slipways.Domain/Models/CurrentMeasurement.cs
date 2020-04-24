using System;

namespace com.b_velop.Slipways.Domain.Models
{
    public class CurrentMeasurement
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public int Trend { get; set; }
        public string StateMnwMhw { get; set; }
        public string StateNswHsw { get; set; }

        public virtual Station Station { get; set; }
        public Guid StationId { get; set; }
    }
}
