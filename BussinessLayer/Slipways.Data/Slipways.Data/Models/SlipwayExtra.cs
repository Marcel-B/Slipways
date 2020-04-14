using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class SlipwayExtra : Entity
    {
        public Guid SlipwayFk { get; set; }

        public Guid ExtraFk { get; set; }

        [ForeignKey("SlipwayFk")]
        public virtual Slipway Slipway { get; set; }

        [ForeignKey("ExtraFk")]
        public virtual Extra Extra { get; set; }
    }
}
