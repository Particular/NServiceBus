namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;

    partial class UnicastBusInternal
    {
        /// <inheritdoc />
        public Task PublishAsync(object message, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull("message", message);
            Guard.AgainstNull("options", options);

            return busImpl.PublishAsync(message, options);
        }


        /// <inheritdoc />
        public Task PublishAsync<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull("messageConstructor", messageConstructor);
            Guard.AgainstNull("options", options);

            return busImpl.PublishAsync(messageConstructor, options);
        }


        /// <inheritdoc />
        public Task SendAsync(object message, NServiceBus.SendOptions options)
        {
            return busImpl.SendAsync(message, options);
        }



        /// <inheritdoc />
        public Task SendAsync<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            return busImpl.SendAsync(messageConstructor, options);
        }

        /// <inheritdoc />
        public void Subscribe(Type eventType, SubscribeOptions options)
        {
            Guard.AgainstNull("eventType", eventType);
            Guard.AgainstNull("options", options);

            busImpl.Subscribe(eventType,options);
        }

        /// <inheritdoc />
        public void Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            Guard.AgainstNull("eventType", eventType);
            Guard.AgainstNull("options", options);

            busImpl.Unsubscribe(eventType, options);
        }

        /// <inheritdoc />
        public Task ReplyAsync(object message, NServiceBus.ReplyOptions options)
        {
            Guard.AgainstNull("message", message);
            Guard.AgainstNull("options", options);

            return busImpl.ReplyAsync(message, options);
        }


        /// <inheritdoc />
        public Task ReplyAsync<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            Guard.AgainstNull("messageConstructor", messageConstructor);
            Guard.AgainstNull("options", options);

            return busImpl.ReplyAsync(messageConstructor, options);
        }

        /// <inheritdoc />
        public Task HandleCurrentMessageLaterAsync()
        {
            return busImpl.HandleCurrentMessageLaterAsync();
        }
        
        /// <inheritdoc />
        public Task ForwardCurrentMessageToAsync(string destination)
        {
            Guard.AgainstNullAndEmpty("destination", destination);
            return busImpl.ForwardCurrentMessageToAsync(destination);
        }
        
        /// <inheritdoc />
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            busImpl.DoNotContinueDispatchingCurrentMessageToHandlers();
        }

        /// <inheritdoc />
        public IMessageContext CurrentMessageContext
        {
            get { return busImpl.CurrentMessageContext; }
        }
    }
}