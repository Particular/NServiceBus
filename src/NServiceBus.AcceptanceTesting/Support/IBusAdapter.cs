namespace NServiceBus.AcceptanceTesting.Support
{
    using System;

    public class IBusAdapter : IBus
    {
        readonly ISendOnlyBus sendOnlyBus;

        public IBusAdapter(ISendOnlyBus sendOnlyBus)
        {
            this.sendOnlyBus = sendOnlyBus;
        }

        public void Dispose()
        {
            sendOnlyBus.Dispose();
        }

        public void Publish(object message)
        {
            sendOnlyBus.Publish(message);
        }

        public void Publish<T>()
        {
            sendOnlyBus.Publish<T>();
        }

        public void Publish<T>(Action<T> messageConstructor)
        {
            sendOnlyBus.Publish(messageConstructor);
        }

        public ICallback Send(object message, SendOptions options)
        {
            return sendOnlyBus.Send(message, options);
        }

        public ICallback Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return sendOnlyBus.Send(messageConstructor, options);
        }

        [Obsolete("", true)]
        ICallback ISendOnlyBus.Send(Address address, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        ICallback ISendOnlyBus.Send<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        ICallback ISendOnlyBus.Send(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        ICallback ISendOnlyBus.Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public void Subscribe(Type messageType)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T>()
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(Type messageType)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe<T>()
        {
            throw new NotImplementedException();
        }

        public void Reply(object message)
        {
            throw new NotImplementedException();
        }

        public void Reply<T>(Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public void Return<T>(T errorEnum)
        {
            throw new NotImplementedException();
        }

        public ICallback SendLocal(object message, SendLocalOptions options)
        {
            throw new NotImplementedException();
        }

        public ICallback SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options)
        {
            throw new NotImplementedException();
        }

        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Defer(DateTime processAt, object message)
        {
            throw new NotImplementedException();
        }

        public void HandleCurrentMessageLater()
        {
            throw new NotImplementedException();
        }

        public void ForwardCurrentMessageTo(string destination)
        {
            throw new NotImplementedException();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            throw new NotImplementedException();
        }

        public IMessageContext CurrentMessageContext { get; private set; }
    }
}
