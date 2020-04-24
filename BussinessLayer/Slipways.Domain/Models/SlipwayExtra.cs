using System;

namespace com.b_velop.Slipways.Domain.Models
{
    public class SlipwayExtra 
    {
        public Guid SlipwayId { get; set; }
        public Guid ExtraId { get; set; }
        public virtual Slipway Slipway { get; set; }
        public virtual Extra Extra { get; set; }
    }
}
