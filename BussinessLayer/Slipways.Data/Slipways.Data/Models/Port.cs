using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class Port : Locationable
    {
        public Port()
        {
            Slipways = new HashSet<Slipway>();
        }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Hompage")]
        [Url]
        public string Url { get; set; }

        [Display(Name = "Straße")]
        public string Street { get; set; }

        [Display(Name = "PLZ")]
        public string Postalcode { get; set; }

        [Display(Name = "Stadt / Ort")]
        public string City { get; set; }

        [Display(Name = "Telefon")]
        [Phone]
        public string Phone { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [JsonIgnore]
        [IgnoreDataMember]
        public virtual ICollection<Slipway> Slipways { get; set; }

        [ForeignKey("WaterFk")]
        public virtual Water Water { get; set; }

        public Guid WaterFk { get; set; }
    }
}
