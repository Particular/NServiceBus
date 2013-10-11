namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Janitor;

    /// <summary>
    /// yeah, we should probably see if we can come up with better names :)
    /// </summary>
    [SkipWeaving]
    public class BehaviorContext : IBehaviorContext, IDisposable
    {
        [ThreadStatic]
        static IBehaviorContext current;

        /// <summary>
        /// Accesses the ambient current <see cref="IBehaviorContext"/> if any
        /// </summary>
        public static IBehaviorContext Current
        {
            get { return current; }
        }

        int traceIndentLevel;

        [SkipWeaving]
        class DisposeAction : IDisposable
        {
            Action whenDisposed;

            public DisposeAction(Action whenDisposed)
            {
                this.whenDisposed = whenDisposed;
            }

            public void Dispose()
            {
                if (whenDisposed == null) return;

                try
                {
                    whenDisposed();
                }
                finally
                {
                    whenDisposed = null;
                }
            }
        }

        readonly List<Tuple<int, string, object[]>> executionTrace = new List<Tuple<int, string, object[]>>();

        public BehaviorContext(TransportMessage transportMessage)
        {
            if (current != null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to establish a new behavior context on {0}, but one was already established: {1} (transport message ID {2})",
                        Thread.CurrentThread.Name, current, current.TransportMessage.Id));
            }
            current = this;
            Set(transportMessage);
        }

        public TransportMessage TransportMessage
        {
            get { return Get<TransportMessage>(); }
        }

        public object[] Messages
        {
            get { return Get<object[]>("NServiceBus.Messages"); }
            set { Set("NServiceBus.Messages", value); }
        }

        public IDisposable TraceContextFor<T>()
        {
            traceIndentLevel++;
            return new DisposeAction(() => traceIndentLevel--);
        }

        public void Trace(string message, params object[] objs)
        {
            executionTrace.Add(Tuple.Create(traceIndentLevel, message, objs));
        }

        public bool DoNotContinueDispatchingMessageToHandlers { get; set; }

        readonly Dictionary<string, object> stash = new Dictionary<string, object>();

        public T Get<T>()
        {
            return Get<T>(typeof(T).FullName);
        }

        public T Get<T>(string key)
        {
            return stash.ContainsKey(key)
                       ? (T) stash[key]
                       : default(T);
        }

        public void Set<T>(T t)
        {
            Set(typeof(T).FullName, t);
        }

        public void Set<T>(string key, T t)
        {
            stash[key] = t;
        }

        public string GetTrace()
        {
            const int spacesPerLevel = 4;
                
            return String.Join(Environment.NewLine,
                               executionTrace.Select(t => new string(' ', spacesPerLevel * t.Item1) + String.Format(t.Item2, t.Item3)));
        }

        public void Dispose()
        {
            current = null;
        }
    }
}