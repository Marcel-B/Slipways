using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class ManufacturerService : Entity
    {
        [ForeignKey("ManufacturerFk")]
        public virtual Manufacturer Manufacturer { get; set; }

        [ForeignKey("ServiceFk")]
        public virtual Service Service { get; set; }

        public Guid ServiceFk { get; set; }

        public Guid ManufacturerFk { get; set; }
    }
}
