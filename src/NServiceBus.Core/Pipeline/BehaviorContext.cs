namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using ObjectBuilder;

    internal abstract class BehaviorContext
    {
        protected BehaviorContext(BehaviorContext parentContext)
        {
            this.parentContext = parentContext;
        }

        public bool ChainAborted { get; private set; }

        public IBuilder Builder
        {
            get
            {
                return Get<IBuilder>();
            }
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
            if (stash.ContainsKey(key))
            {
                return (T)stash[key];
            }

            if (parentContext != null)
            {
                return parentContext.Get<T>(key);
            }

            if (typeof(T).IsValueType)
                return default(T);

            throw new KeyNotFoundException("No item found in behavior context with key: " + key);
        }

        public void Set<T>(T t)
        {
            Set(typeof(T).FullName, t);
        }

        public void Set<T>(string key, T t)
        {
            stash[key] = t;
        }


        readonly BehaviorContext parentContext;

        internal bool handleCurrentMessageLaterWasCalled;

        Dictionary<string, object> stash = new Dictionary<string, object>();
    }
}