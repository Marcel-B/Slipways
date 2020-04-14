using System;
using Slipways.Mobile.Contracts;
using SQLite;

namespace Slipways.Mobile.Data.Models
{
    public class SlipwayExtra : IEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public Guid SlipwayPk { get; set; }
        public Guid ExtraPk { get; set; }
        public DateTime Updated { get; set; }
        public Guid Pk { get; set; }

        public string Name => "";
    }
}
