﻿namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Extensibility;

    public class FakeBus : IBus
    {
        public FakeBusContext Context { get; set; } = new FakeBusContext();

        public IBusContext CreateSendContext()
        {
            return Context;
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

        [Obsolete("", true)]
        public void Return<T>(T errorEnum)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public class FakeBusContext : IBusContext
        {
            int deferWasCalled;

            public ContextBag Extensions { get; }

            public int DeferWasCalled
            {
                get { return deferWasCalled; }
                set { deferWasCalled = value; }
            }

            public TimeSpan DeferDelay { get; private set; } = TimeSpan.MinValue;

            public object DeferedMessage { get; set; }

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

            public Task PublishAsync(object message, PublishOptions options)
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
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
        }
    }
}