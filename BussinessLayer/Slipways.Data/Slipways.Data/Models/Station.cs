using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class Station : Locationable
    {
        public string Number { get; set; }
        public string Shortname { get; set; }
        public string Longname { get; set; }
        public double Km { get; set; }
        public string Agency { get; set; }
        public Guid WaterFk { get; set; }

        [ForeignKey("WaterFk")]
        public virtual Water Water { get; set; }
    }
}
