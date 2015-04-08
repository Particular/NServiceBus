namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class FakeBus : IBus 
    {
        public void Publish(object message)
        {
            throw new NotImplementedException();
        }

        public void Publish<T>()
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(Action<T> messageConstructor)
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

        public ICallback Send(object message, SendOptions options)
        {
            if (options.Delay.HasValue)
            {
                Interlocked.Increment(ref _deferWasCalled);
                deferDelay = options.Delay.Value;
                deferedMessage = message;
                
            }
            return null;
        }

        
        public ICallback Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            throw new NotImplementedException();
        }

        private int _deferWasCalled;
        public int DeferWasCalled
        {
            get { return _deferWasCalled; }
            set { _deferWasCalled = value; }
        }
        private TimeSpan deferDelay = TimeSpan.MinValue;
        public TimeSpan DeferDelay
        {
            get { return deferDelay; }
        }
        object deferedMessage;
        public object DeferedMessage
        {
            get { return deferedMessage; }
        }

        private DateTime _deferProcessAt = DateTime.MinValue;
        public DateTime DeferProcessAt
        {
            get { return _deferProcessAt; }
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

        public IDictionary<string, string> OutgoingHeaders
        {
            get { throw new NotImplementedException(); }
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