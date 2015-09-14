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

        public Task PublishAsync(object message, PublishOptions options)
        {
            return sendOnlyBus.PublishAsync(message, options);
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions options)
        {
            return sendOnlyBus.PublishAsync(messageConstructor, options);
        }

        public Task SendAsync(object message, SendOptions options)
        {
            return sendOnlyBus.SendAsync(message, options);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return sendOnlyBus.SendAsync(messageConstructor, options);
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
