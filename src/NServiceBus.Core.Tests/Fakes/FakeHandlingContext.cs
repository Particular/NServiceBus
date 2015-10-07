namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Extensibility;

    public class FakeHandlingContext : IMessageHandlerContext
    {
        int deferWasCalled;

        public string MessageId { get; }
        public string ReplyToAddress { get; }
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }
        public ContextBag Extensions { get; }

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

        public Task ReplyAsync(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
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

        public int DeferWasCalled
        {
            get { return deferWasCalled; }
            set { deferWasCalled = value; }
        }

        public TimeSpan DeferDelay { get; private set; } = TimeSpan.MinValue;

        public object DeferedMessage { get; set; }

        
    }
}