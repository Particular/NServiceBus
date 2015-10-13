namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides access to the dispatchers.
    /// </summary>
    class TransportDispatcher
    {
        IDispatchMessages defaultDispatcher;
        IReadOnlyCollection<Tuple<IDispatchMessages, TransportDefinition>> dispatchers;

        internal TransportDispatcher(
            IDispatchMessages defaultDispatcher,
            IReadOnlyCollection<Tuple<IDispatchMessages, TransportDefinition>> dispatchers)
        {
            if (defaultDispatcher == null)
            {
                throw new InvalidOperationException("Default transport has to provide either a dispatcher.");
            }
            this.defaultDispatcher = defaultDispatcher;
            this.dispatchers = dispatchers;
        }

        public Task UseDispatcher(Func<IDispatchMessages, Task> action)
        {
            if (defaultDispatcher == null)
            {
                throw new InvalidOperationException("Selected transport does not provide a dispatcher.");
            }
            return action(defaultDispatcher);
        }
        
        public Task UseDispatcherFor(TransportDefinition transport, Func<IDispatchMessages, Task> action)
        {
            var dispatcher = dispatchers.Single(d => d.Item2 == transport).Item1;
            return action(dispatcher);
        }
    }
}