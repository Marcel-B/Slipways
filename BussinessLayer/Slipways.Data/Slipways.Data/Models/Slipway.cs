using com.b_velop.Slipways.Data.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class Slipway : Locationable, IEntity
    {
        public Slipway()
        {
            SlipwayExtras = new HashSet<SlipwayExtra>();
        }

        [Display(Name = "Name")]
        public string Name { get; set; }

        public Guid? PortFk { get; set; }

        [ForeignKey("PortFk")]
        public virtual Port Port { get; set; }

        public Guid WaterFk { get; set; }

        [ForeignKey("WaterFk")]
        public virtual Water Water { get; set; }

        [Display(Name = "Bewertung")]
        public int Rating { get; set; }

        [Display(Name = "Kommentar")]
        public string Comment { get; set; }

        [Display(Name = "Straße")]
        public string Street { get; set; }

        [Display(Name = "PLZ")]
        public string Postalcode { get; set; }

        [Display(Name = "Stadt / Ort")]
        public string City { get; set; }

        [Display(Name = "Land")]
        public string Country { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        [Display(Name = "Kosten (-1 für unbekannt)")]
        public decimal Costs { get; set; }

        [Display(Name = "Pro")]
        public string Pro { get; set; }

        [Display(Name = "Kontra")]
        public string Contra { get; set; }

        public virtual ICollection<SlipwayExtra> SlipwayExtras { get; set; }
    }
}
