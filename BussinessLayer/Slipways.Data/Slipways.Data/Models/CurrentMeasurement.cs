using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class CurrentMeasurement : Entity
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public int Trend { get; set; }
        public string StateMnwMhw { get; set; }
        public string StateNswHsw { get; set; }

        [ForeignKey("StationFk")]
        public virtual Station Station { get; set; }

        public Guid StationFk { get; set; }
    }
}
