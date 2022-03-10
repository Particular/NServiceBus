namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class MessageSession : IMessageSession
    {
        public MessageSession(RootContext context)
        {
            this.context = context;
            messageOperations = context.Get<MessageOperations>();
        }

        public async Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
            await messageOperations.Send(new BehaviorContext(context, linkedTokenSource.Token), message, sendOptions).ConfigureAwait(false);
        }

        public async Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
            await messageOperations.Send(new BehaviorContext(context, linkedTokenSource.Token), messageConstructor, sendOptions).ConfigureAwait(false);
        }

        public async Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
            await messageOperations.Publish(new BehaviorContext(context, linkedTokenSource.Token), message, publishOptions).ConfigureAwait(false);
        }

        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
            await messageOperations.Publish(new BehaviorContext(context, linkedTokenSource.Token), messageConstructor, publishOptions).ConfigureAwait(false);
        }

        public async Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(subscribeOptions), subscribeOptions);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
            await messageOperations.Subscribe(new BehaviorContext(context, linkedTokenSource.Token), eventType, subscribeOptions).ConfigureAwait(false);
        }

        public async Task SubscribeAll(Type[] eventTypes, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
        {
            // set a flag on the context so that subscribe implementations know which send API was used.
            subscribeOptions.Context.Set(SubscribeAllFlagKey, true);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
            await messageOperations.Subscribe(new BehaviorContext(context, linkedTokenSource.Token), eventTypes, subscribeOptions).ConfigureAwait(false);
        }

        public async Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(unsubscribeOptions), unsubscribeOptions);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
            await messageOperations.Unsubscribe(new BehaviorContext(context, linkedTokenSource.Token), eventType, unsubscribeOptions).ConfigureAwait(false);
        }

        RootContext context;
        MessageOperations messageOperations;

        internal const string SubscribeAllFlagKey = "NServiceBus.SubscribeAllFlag";
    }
}