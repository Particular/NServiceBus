namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Janitor;

    [SkipWeaving]
    class ObservableList<T> : IObservable<T>, IDisposable
    {
        List<IObserver<T>> observers;
        bool isDisposed;
        object gate = new object();

        public ObservableList()
        {
            observers = new List<IObserver<T>>();
        }

        public void Dispose()
        {
            lock (gate)
            {
                foreach (var observer in observers)
                {
                    observer.OnCompleted();
                }

                observers = null;
                isDisposed = true;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer", "observer is null.");
            }
            
            lock (gate)
            {
                CheckDisposed();

                observers.Add(observer);

                return new Unsubscriber(this, observer);
            }
        }

        public void Add(T step)
        {
            // If the observers list was immutable we could remove this lock
            lock (gate)
            {
                CheckDisposed();

                foreach (var observer in observers)
                {
                    observer.OnNext(step);
                }
            }
        }

        void Unsubscribe(IObserver<T> observer)
        {
            lock (gate)
            {
                observers.Remove(observer);
            }
        }

        void CheckDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }
        }

        [SkipWeaving]
        class Unsubscriber : IDisposable
        {
            ObservableList<T> observableList;
            IObserver<T> observer;

            public Unsubscriber(ObservableList<T> observableList, IObserver<T> observer)
            {
                this.observableList = observableList;
                this.observer = observer;
            }

            public void Dispose()
            {
                var o = Interlocked.Exchange(ref observer, null);
                if (o != null)
                {
                    observableList.Unsubscribe(observer);
                    observableList = null;
                }
            }
        }
    }
}
