namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Extensibility;

    public class FakeBus : IBus
    {
        DateTime _deferProcessAt = DateTime.MinValue;
        int _deferWasCalled;
        TimeSpan deferDelay = TimeSpan.MinValue;

        public int DeferWasCalled
        {
            get { return _deferWasCalled; }
            set { _deferWasCalled = value; }
        }

        public TimeSpan DeferDelay
        {
            get { return deferDelay; }
        }

        public object DeferedMessage { get; set; }

        public DateTime DeferProcessAt
        {
            get { return _deferProcessAt; }
        }

        public IDictionary<string, string> OutgoingHeaders
        {
            get { throw new NotImplementedException(); }
        }

        public void Publish(object message,PublishOptions options)
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(Action<T> messageConstructor,PublishOptions options)
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

        public void Send(object message, SendOptions options)
        {
            
        }

        public void Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback Send(Address address, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback Send(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }


        [Obsolete("", true)]
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public void SendLocal(object message, SendLocalOptions options)
        {
            ApplyDelayedDeliveryConstraintBehavior.State state;

            if (options.GetExtensions().TryGet(out state))
            {
                var delayConstraint = state.RequestedDelay as DelayDeliveryWith;

                if (delayConstraint != null)
                {
                    Interlocked.Increment(ref _deferWasCalled);
                    deferDelay = delayConstraint.Delay;
                    DeferedMessage = message;   
                }
            }
        }

        public void SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options)
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

        public void Reply(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public void Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        [Obsolete("", true)]
        public void Return<T>(T errorEnum)
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

        public IMessageContext CurrentMessageContext
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
        }
    }
}