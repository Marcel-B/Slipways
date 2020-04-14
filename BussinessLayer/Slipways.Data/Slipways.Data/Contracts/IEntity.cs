using System;

namespace com.b_velop.Slipways.Data.Contracts
{
    public interface IEntity
    {
        Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
