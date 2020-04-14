using System;

namespace Slipways.Mobile.Contracts
{
    public interface IEntity
    {
        Guid Pk { get; set; }
        int Id { get; set; }
        string Name { get; }
    }
}
