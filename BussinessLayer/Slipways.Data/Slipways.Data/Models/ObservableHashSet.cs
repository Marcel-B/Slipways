using System;
using System.Collections.Generic;
using System.Text;

namespace com.b_velop.Slipways.Data.Models
{
    public class ObservableHashSet<T> : HashSet<T>, IObservable<T>
    {
        public IDisposable Subscribe(
            IObserver<T> observer)
        {
            throw new NotImplementedException();
        }
    }
}
