using System;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.GrQl.Data.Models
{
    public class ManufacturerDto : Manufacturer
    {
        public Guid ServiceId { get; set; }
    }
}