namespace NServiceBus.AcceptanceTesting.Support
{
    using System;

    public class IBusAdapter : IBus
    {
        ISendOnlyBus sendOnlyBus;

        public IBusAdapter(ISendOnlyBus sendOnlyBus)
        {
            this.sendOnlyBus = sendOnlyBus;
        }

        public void Dispose()
        {
            sendOnlyBus.Dispose();
        }

        public void Publish(object message, PublishOptions options)
        {
            sendOnlyBus.Publish(message, options);
        }

        public void Publish<T>(Action<T> messageConstructor, PublishOptions options)
        {
            sendOnlyBus.Publish(messageConstructor, options);
        }

        public void Send(object message, SendOptions options)
        {
            sendOnlyBus.Send(message, options);
        }

        public void Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            sendOnlyBus.Send(messageConstructor, options);
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

        [Obsolete("", true)]
        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        ICallback ISendOnlyBus.Send(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        ICallback ISendOnlyBus.Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public void Reply(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public void Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public void Subscribe(Type eventType, SubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public void Return<T>(T errorEnum)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
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
