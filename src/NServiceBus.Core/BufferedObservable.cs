namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;

    [SkipWeaving]
    class BufferedObservable<T> : IObservable<T>, IDisposable
    {
        List<IObserver<T>> observers;
        BlockingCollection<Action<IObserver<T>>> buffer; 
        bool isDisposed;
        int disposeSignaled;
        ReaderWriterLockSlim observerLock = new ReaderWriterLockSlim();
        Task dispatchTask;

        public BufferedObservable(int bufferSize = 10)
        {
            observers = new List<IObserver<T>>();
            buffer = new BlockingCollection<Action<IObserver<T>>>(bufferSize);
            dispatchTask = Task.Factory.StartNew(Dispatch);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
            {
                return;
            }

            buffer.Add(x => x.OnCompleted());
            buffer.CompleteAdding();
            dispatchTask.Wait();

            observerLock.Dispose();
            observerLock = null;
            observers = null;
            buffer = null;
            isDisposed = true;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer", "observer is null.");
            }

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
            if (disposeSignaled == 1)
            {
                return;
            }
            buffer.Add(x => x.OnNext(value));
        }

        void Dispatch()
        {
            var reader = buffer.GetConsumingEnumerable();
            foreach (var action in reader)
            {
                observerLock.EnterReadLock();
                try
                {
                    foreach (var observer in observers)
                    {
                        action(observer);
                    }
                }
                finally
                {
                    observerLock.ExitReadLock();
                }
            }
        }

        public void OnError(Exception ex)
        {
            if (disposeSignaled == 1)
            {
                return;
            }
            CheckDisposed();
            buffer.Add(x => x.OnError(ex));
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
            BufferedObservable<T> observable;
            IObserver<T> observer;

            public Unsubscriber(BufferedObservable<T> observable, IObserver<T> observer)
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