using System;
using Slipways.Mobile.Contracts;
using SQLite;

namespace Slipways.Mobile.Data.Models
{
    public class User : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public Guid Pk { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Version { get; set; }
    }
}
