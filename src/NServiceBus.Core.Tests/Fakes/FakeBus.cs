namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Extensibility;

    public class FakeBus : IBus
    {
        int deferWasCalled;

        public int DeferWasCalled
        {
            get { return deferWasCalled; }
            set { deferWasCalled = value; }
        }

        public TimeSpan DeferDelay { get; private set; } = TimeSpan.MinValue;

        public object DeferedMessage { get; set; }

        public DateTime DeferProcessAt { get; } = DateTime.MinValue;

        public IDictionary<string, string> OutgoingHeaders
        {
            get { throw new NotImplementedException(); }
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions options)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(object message, SendOptions options)
        {
            ApplyDelayedDeliveryConstraintBehavior.State state;

            if (options.GetExtensions().TryGet(out state))
            {
                var delayConstraint = state.RequestedDelay as DelayDeliveryWith;

                if (delayConstraint != null)
                {
                    Interlocked.Increment(ref deferWasCalled);
                    DeferDelay = delayConstraint.Delay;
                    DeferedMessage = message;
                }
            }
            return Task.FromResult(0);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback SendAsync(Address address, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback SendAsync<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback SendAsync(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback SendAsync(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback SendAsync<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }


        [Obsolete("", true)]
        public ICallback SendAsync<T>(Address address, string correlationId, Action<T> messageConstructor)
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

        [Obsolete("", true)]
        public void Return<T>(T errorEnum)
        {
            throw new NotImplementedException();
        }

        public Task HandleCurrentMessageLaterAsync()
        {
            throw new NotImplementedException();
        }

        public Task ForwardCurrentMessageToAsync(string destination)
        {
            throw new NotImplementedException();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            throw new NotImplementedException();
        }

        public IMessageContext CurrentMessageContext
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
        }
    }
}