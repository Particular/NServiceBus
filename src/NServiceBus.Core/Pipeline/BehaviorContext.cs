namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;

    /// <summary>
    ///     yeah, we should probably see if we can come up with better names :)
    /// </summary>
    internal class BehaviorContext : IDisposable
    {
        public BehaviorContext(IBuilder builder, TransportMessage transportMessage, BehaviorContextStacker contextStacker)
        {
            this.contextStacker = contextStacker;
            Builder = builder;
            handleCurrentMessageLaterWasCalled = false;

            contextStacker.Push(this);

            Set(transportMessage);
        }

        public TransportMessage TransportMessage
        {
            get { return Get<TransportMessage>(); }
        }

        public bool ChainAborted { get; private set; }

        public IBuilder Builder { get; private set; }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void AbortChain()
        {
            ChainAborted = true;
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

        public void DisposeManaged()
        {
            // Pop the stack.
            contextStacker.Pop();
        }

        readonly BehaviorContextStacker contextStacker;

        internal bool handleCurrentMessageLaterWasCalled;

        Dictionary<string, object> stash = new Dictionary<string, object>();
    }
}