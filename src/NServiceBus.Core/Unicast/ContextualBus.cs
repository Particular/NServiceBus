namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using Pipeline;

    [SkipWeaving]
    internal partial class ContextualBus : IBus, IContextualBus
    {
        public ContextualBus(BehaviorContextStacker contextStacker, StaticBus bus)
        {
            this.contextStacker = contextStacker;
            this.bus = bus;
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.PublishAsync"/>
        /// </summary>
        public Task PublishAsync<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            return bus.PublishAsync(messageConstructor, options, incomingContext);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.PublishAsync"/>
        /// </summary>
        public Task PublishAsync(object message, NServiceBus.PublishOptions options)
        {
            return bus.PublishAsync(message, options, incomingContext);
        }

        /// <summary>
        /// <see cref="IBus.SubscribeAsync"/>
        /// </summary>
        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return bus.SubscribeAsync(eventType, options, incomingContext);
        }

        /// <summary>
        /// <see cref="IBus.UnsubscribeAsync"/>
        /// </summary>
        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return bus.UnsubscribeAsync(eventType, options, incomingContext);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            return bus.SendAsync(messageConstructor, options, incomingContext);
        }

        public Task SendAsync(object message, NServiceBus.SendOptions options)
        {
            return bus.SendAsync(message, options, incomingContext);
        }

        [Obsolete("", true)]
        public IMessageContext CurrentMessageContext
        {
            get { throw new NotImplementedException(); }
        }

        BehaviorContext incomingContext => contextStacker.GetCurrentOrRootContext();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        BehaviorContextStacker contextStacker;
        StaticBus bus;
    }
}