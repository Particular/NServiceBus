namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
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
            get { return behaviorContextStack.Peek(); }
        }

        public BehaviorContext(IBuilder builder, TransportMessage transportMessage)
        {
            Builder = builder;
            handleCurrentMessageLaterWasCalled = false;

            behaviorContextStack = behaviorContextStack ?? new Stack<BehaviorContext>();

            behaviorContextStack.Push(this);
            Set(transportMessage);
        }

        public TransportMessage TransportMessage
        {
            get { return Get<TransportMessage>(); }
        }

        public void AbortChain()
        {
            ChainAborted = true;
        }

        public bool ChainAborted { get; private set; }

        public IBuilder Builder { get; private set; }

        public T Get<T>()
        {
            return Get<T>(typeof(T).FullName);
        }

        public T Get<T>(string key)
        {
            return stash.ContainsKey(key)
                       ? (T)stash[key]
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

        public void Dispose()
        {
            // Pop the stack.
            behaviorContextStack.Pop();
        }

        [ThreadStatic]
        static Stack<BehaviorContext> behaviorContextStack;

        internal bool handleCurrentMessageLaterWasCalled;

        Dictionary<string, object> stash = new Dictionary<string, object>();
    }
}