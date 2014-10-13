namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Janitor;

    [SkipWeaving]
    class ObservableList<T> : IObservable<T>, IDisposable
    {
        public ObservableList()
        {
            observers = new List<IObserver<T>>();
        }

        public void Dispose()
        {
            foreach (var observer in observers)
            {
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }

            return new Unsubscriber<T>(observers, observer);
        }

        public void Add(T step)
        {
            foreach (var observer in observers)
            {
                observer.OnNext(step);
            }
        }

        List<IObserver<T>> observers;

        [SkipWeaving]
        class Unsubscriber<S> : IDisposable
        {
            public Unsubscriber(List<IObserver<S>> observers, IObserver<S> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (observer != null && observers.Contains(observer))
                {
                    observers.Remove(observer);
                }
            }

            IObserver<S> observer;
            List<IObserver<S>> observers;
        }
    }
}