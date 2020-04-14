using Prism.Events;
using Slipways.Mobile.Helpers;

namespace Slipways.Mobile.Events
{
    public class UpdateReadyEvent<T> : PubSubEvent<DataUpdateEventArgs<T>> { }
}
