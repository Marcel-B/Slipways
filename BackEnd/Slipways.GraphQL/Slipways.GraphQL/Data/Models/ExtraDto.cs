using System;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.GrQl.Data.Models
{
    public class ExtraDto : Extra
    {
        public Guid SlipwayId { get; set; }
    }
}