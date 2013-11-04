namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;

    internal class BehaviorContext : IDisposable
    {
        public BehaviorContext(PipelineFactory pipelineFactory)
        {
            this.pipelineFactory = pipelineFactory;
        }


        public PipelineFactory PipelineFactory
        {
            get
            {
                return pipelineFactory;
            }
        }

        public bool ChainAborted { get; private set; }

        public IBuilder Builder {
            get
            {
                return pipelineFactory.CurrentBuilder;
            } }

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

    
        readonly PipelineFactory pipelineFactory;

        internal bool handleCurrentMessageLaterWasCalled;

        Dictionary<string, object> stash = new Dictionary<string, object>();
    }
}