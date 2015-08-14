namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

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

        public Task Publish(object message,PublishOptions options)
        {
            return sendOnlyBus.Publish(message,options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions options)
        {
            return sendOnlyBus.Publish(messageConstructor, options);
        }

        public Task Send(object message, SendOptions options)
        {
            return sendOnlyBus.Send(message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
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

        public Task Reply(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
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

        public Task HandleCurrentMessageLater()
        {
            throw new NotImplementedException();
        }

        public Task ForwardCurrentMessageTo(string destination)
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
