namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Janitor;
    using ObjectBuilder;

    /// <summary>
    /// yeah, we should probably see if we can come up with better names :)
    /// </summary>
    [SkipWeaving]
    class BehaviorContext : IDisposable
    {

        /// <summary>
        /// Accesses the ambient current <see cref="IBehaviorContext"/> if any
        /// </summary>
        public static BehaviorContext Current
        {
            get { return current; }
        }

        public BehaviorContext(IBuilder builder,TransportMessage transportMessage)
        {
            this.builder = builder;
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
            set
            {
                Set("NServiceBus.Messages", value);
            }
        }

        public Action TraceContextFor()
        {
            traceIndentLevel++;
            return () => traceIndentLevel--;
        }

        public void Trace(string message, params object[] objs)
        {
            executionTrace.Add(Tuple.Create(traceIndentLevel, message, objs));
        }

        public void AbortChain()
        {
            aborted = true;
        }

        public bool ChainAborted
        {
            get
            {
                return aborted;
            }
        }

        public IBuilder Builder
        {
            get
            {
                return builder;
            }
        }

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
            var spacesPerLevel = 4;
                
            return String.Join(Environment.NewLine,
                               executionTrace.Select(t => new string(' ', spacesPerLevel * t.Item1) + String.Format(t.Item2, t.Item3)));
        }

        public void Dispose()
        {
            current = null;
        }

        [ThreadStatic]
        static BehaviorContext current;

        Dictionary<string, object> stash = new Dictionary<string, object>();


        int traceIndentLevel;

        List<Tuple<int, string, object[]>> executionTrace = new List<Tuple<int, string, object[]>>();

        readonly IBuilder builder;

        bool aborted;

    }
}