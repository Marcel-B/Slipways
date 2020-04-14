using com.b_velop.Slipways.Data.Contracts;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace com.b_velop.Slipways.Data.Models
{
    [Serializable]
    public abstract class Entity : IEntity
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [Display(Name = "Erstellt")]
        public DateTime Created { get; set; }

        [Display(Name = "Geändert")]
        public DateTime? Updated { get; set; }
    }
}
