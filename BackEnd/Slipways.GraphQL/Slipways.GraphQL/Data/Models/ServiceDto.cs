using System;
using com.b_velop.Slipways.Domain.Models;

namespace com.b_velop.Slipways.GrQl.Data.Models
{
    public class ServiceDto : Service
    {
        public Guid ManufacturerId { get; set; }
    }
}