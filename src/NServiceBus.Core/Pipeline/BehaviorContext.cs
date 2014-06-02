namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;

    /// <summary>
    /// Base class for a pipeline behavior.
    /// </summary>
    public abstract class BehaviorContext
    {
        /// <summary>
        /// Create an instance of <see cref="BehaviorContext"/>.
        /// </summary>
        /// <param name="parentContext"></param>
        protected BehaviorContext(BehaviorContext parentContext)
        {
            this.parentContext = parentContext;
        }

        /// <summary>
        /// The current <see cref="IBuilder"/>
        /// </summary>
        public IBuilder Builder
        {
            get { return Get<IBuilder>(); }
        }

        internal void SetChain(dynamic chain)
        {
            this.chain = chain;
        }

        internal IDisposable CreateSnapshotRegion()
        {
            return new SnapshotRegion(chain);
        }

        public T Get<T>()
        {
            return Get<T>(typeof(T).FullName);
        }

        public bool TryGet<T>(out T result)
        {
            return TryGet(typeof(T).FullName, out result);
        }

        public bool TryGet<T>(string key, out T result)
        {
            object value;
            if (stash.TryGetValue(key, out value))
            {
                result = (T) value;
                return true;
            }

            if (parentContext != null)
            {
                return parentContext.TryGet(key, out result);
            }

            if (typeof(T).IsValueType)
            {
                result = default(T);
                return true;
            }

            result = default(T);
            return false;
        }

        public T Get<T>(string key)
        {
            T result;

            if (!TryGet(key, out result))
            {
                throw new KeyNotFoundException("No item found in behavior context with key: " + key);
            }

            return result;
        }

        public void Set<T>(T t)
        {
            Set(typeof(T).FullName, t);
        }

        public void Set<T>(string key, T t)
        {
            stash[key] = t;
        }

        public void Remove<T>()
        {
            Remove(typeof(T).FullName);
        }

        public void Remove(string key)
        {
            stash.Remove(key);
        }

        protected readonly BehaviorContext parentContext;
        dynamic chain;

        internal bool handleCurrentMessageLaterWasCalled;

        Dictionary<string, object> stash = new Dictionary<string, object>();
    }
}