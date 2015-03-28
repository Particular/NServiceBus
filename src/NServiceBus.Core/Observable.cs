namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Janitor;

    [SkipWeaving]
    class Observable<T> : IObservable<T>, IDisposable
    {
        List<IObserver<T>> observers;
        bool isDisposed;
        int disposeSignaled;
        ReaderWriterLockSlim observerLock = new ReaderWriterLockSlim();

        public Observable()
        {
            observers = new List<IObserver<T>>();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
            {
                return;
            }

            observerLock.EnterReadLock();
            try
            {
                foreach (var observer in observers)
                {
                    observer.OnCompleted();
                }
            }
            finally
            {
                observerLock.ExitReadLock();
            }

            observerLock.Dispose();
            observerLock = null;
            observers = null;
            isDisposed = true;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            Guard.AgainstNull(observer, "observer");

            CheckDisposed();

            observerLock.EnterWriteLock();
            try
            {
                observers.Add(observer);
            }
            finally
            {
                observerLock.ExitWriteLock();
            }

            return new Unsubscriber(this, observer);
        }

        public void OnNext(T value)
        {
            CheckDisposed();

            observerLock.EnterReadLock();
            try
            {
                foreach (var observer in observers)
                {
                    observer.OnNext(value);
                }
            }
            finally
            {
                observerLock.ExitReadLock();
            }
        }

        public void OnError(Exception ex)
        {
            CheckDisposed();

            observerLock.EnterReadLock();
            try
            {
                foreach (var observer in observers)
                {
                    observer.OnError(ex);
                }
            }
            finally
            {
                observerLock.ExitReadLock();
            }
        }

        public void OnCompleted()
        {
            CheckDisposed();

            observerLock.EnterReadLock();
            try
            {
                foreach (var observer in observers)
                {
                    observer.OnCompleted();
                }
            }
            finally
            {
                observerLock.ExitReadLock();
            }
        }

        void Unsubscribe(IObserver<T> observer)
        {
            observerLock.EnterWriteLock();
            try
            {
                observers.Remove(observer);
            }
            finally
            {
                observerLock.ExitWriteLock();
            }
        }

        void CheckDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Observable");
            }
        }

        [SkipWeaving]
        class Unsubscriber : IDisposable
        {
            Observable<T> observable;
            IObserver<T> observer;

            public Unsubscriber(Observable<T> observable, IObserver<T> observer)
            {
                this.observable = observable;
                this.observer = observer;
            }

            public void Dispose()
            {
                var o = Interlocked.Exchange(ref observer, null);
                if (o != null)
                {
                    observable.Unsubscribe(observer);
                    observable = null;
                }
            }
        }

    }
}