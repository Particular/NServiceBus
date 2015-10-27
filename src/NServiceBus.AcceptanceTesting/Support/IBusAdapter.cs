namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    public class IBusAdapter : IStartableBus 
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

       
        public Task ReplyAsync(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public IBusContext CreateSendContext()
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

        [Obsolete("", true)]
        public IMessageContext CurrentMessageContext { get; }

        public Task<IBus> StartAsync()
        {
            return Task.FromResult((IBus)this);
        }
    }
}
