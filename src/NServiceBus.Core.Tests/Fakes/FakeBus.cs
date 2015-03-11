﻿namespace NServiceBus.Core.Tests.Fakes
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

        public ICallback SendLocal(object message)
        {
            throw new NotImplementedException();
        }

        public ICallback SendLocal<T>(Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(object message, SendContext context)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(Action<T> messageConstructor, SendContext context)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(string destination, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Defer(TimeSpan delay, object message)
        {
            return Defer(delay,new []{message});
        }

        public ICallback Defer(TimeSpan delay, params object[] messages)
        {
            Interlocked.Increment(ref _deferWasCalled);
            _deferDelay = delay;
            _deferMessages = messages;
            return null;
        }

        private int _deferWasCalled;
        public int DeferWasCalled
        {
            get { return _deferWasCalled; }
            set { _deferWasCalled = value; }
        }
        private TimeSpan _deferDelay = TimeSpan.MinValue;
        public TimeSpan DeferDelay
        {
            get { return _deferDelay; }
        }
        private object[] _deferMessages;
        public object[] DeferMessages
        {
            get { return _deferMessages; }
        }

        private DateTime _deferProcessAt = DateTime.MinValue;
        public DateTime DeferProcessAt
        {
            get { return _deferProcessAt; }
        }

        public ICallback Defer(DateTime processAt, object message)
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