namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;

    partial class UnicastBusInternal
    {
        /// <inheritdoc />
        public Task PublishAsync(object message, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            return bus.PublishAsync(message, options, rootContext);
        }

        /// <inheritdoc />
        public Task PublishAsync<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(options), options);

            return bus.PublishAsync(messageConstructor, options, rootContext);
        }

        /// <inheritdoc />
        public Task SendAsync(object message, NServiceBus.SendOptions options)
        {
            return bus.SendAsync(message, options, rootContext);
        }

        /// <inheritdoc />
        public Task SendAsync<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            return bus.SendAsync(messageConstructor, options, rootContext);
        }

        /// <inheritdoc />
        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(options), options);

            return bus.SubscribeAsync(eventType, options, rootContext);
        }

        /// <inheritdoc />
        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(options), options);

            return bus.UnsubscribeAsync(eventType, options, rootContext);
        }
    }
}