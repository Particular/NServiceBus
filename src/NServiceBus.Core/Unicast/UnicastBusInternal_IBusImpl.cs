namespace NServiceBus.Unicast
{
    using System;

    partial class UnicastBusInternal
    {
        /// <inheritdoc />
        public void Publish(object message, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull("message", message);
            Guard.AgainstNull("options", options);

            busImpl.Publish(message, options);
        }


        /// <inheritdoc />
        public void Publish<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull("messageConstructor", messageConstructor);
            Guard.AgainstNull("options", options);

            busImpl.Publish(messageConstructor, options);
        }


        /// <inheritdoc />
        public void Send(object message, NServiceBus.SendOptions options)
        {
            busImpl.Send(message, options);
        }



        /// <inheritdoc />
        public void Send<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            busImpl.Send(messageConstructor, options);
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
        public void Reply(object message, NServiceBus.ReplyOptions options)
        {
            Guard.AgainstNull("message", message);
            Guard.AgainstNull("options", options);

            busImpl.Reply(message, options);
        }


        /// <inheritdoc />
        public void Reply<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            Guard.AgainstNull("messageConstructor", messageConstructor);
            Guard.AgainstNull("options", options);

            busImpl.Reply(messageConstructor, options);
        }

        /// <inheritdoc />
        public void HandleCurrentMessageLater()
        {
            busImpl.HandleCurrentMessageLater();
        }
        
        /// <inheritdoc />
        public void ForwardCurrentMessageTo(string destination)
        {
            Guard.AgainstNullAndEmpty("destination", destination);
            busImpl.ForwardCurrentMessageTo(destination);
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