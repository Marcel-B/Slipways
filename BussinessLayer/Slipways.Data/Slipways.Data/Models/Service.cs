using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class Service : Locationable
    {
        public Service()
        {
            Manufacturers = new HashSet<Manufacturer>();
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

        [NotMapped]
        public Guid ManufacturerFk { get; set; }

        [NotMapped]
        public virtual ICollection<Manufacturer> Manufacturers { get; set; }
    }
}
