using System;

namespace NServiceBus.Unicast
{
    public partial class UnicastBus
    {

        /// <inheritdoc />
        public void Publish(object message, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull(message, "message");
            Guard.AgainstNull(options, "options");

            busImpl.Publish(message, options);
        }


        /// <inheritdoc />
        public void Publish<T>(Action<T> messageConstructor, NServiceBus.PublishOptions options)
        {
            Guard.AgainstNull(messageConstructor, "messageConstructor");
            Guard.AgainstNull(options, "options");

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
        public void Subscribe(Type messageType)
        {
            Guard.AgainstNull(messageType, "messageType");
            busImpl.Subscribe(messageType);
        }


        /// <inheritdoc />
        public void Subscribe<T>()
        {
            busImpl.Subscribe<T>();
        }


        /// <inheritdoc />
        public void Unsubscribe(Type messageType)
        {
            Guard.AgainstNull(messageType, "messageType");
            busImpl.Unsubscribe(messageType);
        }


        /// <inheritdoc />
        public void Unsubscribe<T>()
        {
            busImpl.Unsubscribe<T>();
        }


        /// <inheritdoc />
        public void Reply(object message, NServiceBus.ReplyOptions options)
        {
            Guard.AgainstNull(message, "message");
            Guard.AgainstNull(options, "options");

            busImpl.Reply(message, options);
        }


        /// <inheritdoc />
        public void Reply<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            Guard.AgainstNull(messageConstructor, "messageConstructor");
            Guard.AgainstNull(options, "options");

            busImpl.Reply(messageConstructor, options);
        }


        /// <inheritdoc />
        public void SendLocal(object message, SendLocalOptions options)
        {
            busImpl.SendLocal(message, options);
        }


        /// <inheritdoc />
        public void SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options)
        {
            busImpl.SendLocal(messageConstructor, options);
        }


        /// <inheritdoc />
        public void HandleCurrentMessageLater()
        {
            busImpl.HandleCurrentMessageLater();
        }
        
        /// <inheritdoc />
        public void ForwardCurrentMessageTo(string destination)
        {
            Guard.AgainstNullAndEmpty(destination, "destination");
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