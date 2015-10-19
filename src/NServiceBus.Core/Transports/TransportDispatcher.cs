namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides access to the dispatchers.
    /// </summary>
    public class TransportDispatcher
    {
        IDispatchMessages defaultDispatcher;
        IReadOnlyCollection<Tuple<IDispatchMessages, string>> dispatchers;

        internal TransportDispatcher(
            IDispatchMessages defaultDispatcher,
            IReadOnlyCollection<Tuple<IDispatchMessages, string>> dispatchers)
        {
            if (defaultDispatcher == null)
            {
                throw new InvalidOperationException("Default transport has to provide either a dispatcher.");
            }
            this.defaultDispatcher = defaultDispatcher;
            this.dispatchers = dispatchers;
        }

        /// <summary>
        /// Uses the default dispatcher.
        /// </summary>
        /// <param name="action">A callback that makes use of the default dispatcher.</param>
        /// <returns></returns>
        public Task UseDefaultDispatcher(Func<IDispatchMessages, Task> action)
        {
            if (defaultDispatcher == null)
            {
                throw new InvalidOperationException("Selected transport does not provide a dispatcher.");
            }
            return action(defaultDispatcher);
        }

        /// <summary>
        /// Allows to use a specific transport dispatcher.
        /// </summary>
        /// <param name="transport">Specific transport.</param>
        /// <param name="action">A callback that makes use of the selected dispatcher.</param>
        /// <returns></returns>
        public Task UseDispatcher(string transport, Func<IDispatchMessages, Task> action)
        {
            var dispatcher = dispatchers.Single(d => d.Item2 == transport).Item1;
            return action(dispatcher);
        }
    }
}