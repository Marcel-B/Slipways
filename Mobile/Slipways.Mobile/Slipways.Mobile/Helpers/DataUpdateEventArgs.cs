using System.Collections.Generic;

namespace Slipways.Mobile.Helpers
{
    public class DataUpdateEventArgs<T>
    {
        public IEnumerable<T> Data;
        public string Type;
    }
}
