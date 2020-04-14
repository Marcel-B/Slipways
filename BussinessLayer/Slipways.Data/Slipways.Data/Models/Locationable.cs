using System;
using System.ComponentModel.DataAnnotations;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public abstract class Locationable : Entity
    {
        [Display(Name = "Breitengrad")]
        public double Latitude { get; set; }
        
        [Display(Name = "Längengrad")]
        public double Longitude { get; set; }
    }
}
