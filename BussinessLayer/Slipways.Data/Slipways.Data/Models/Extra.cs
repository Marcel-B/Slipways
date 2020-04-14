using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public class Extra : Entity
    {
        public Extra()
        {
            SlipwayExtras = new HashSet<SlipwayExtra>();
        }

        [Display(Name = "Name")]
        public string Name { get; set; }

        public virtual ICollection<SlipwayExtra> SlipwayExtras { get; set; }

        public Guid SlipwayFk { get; set; }
    }
}
