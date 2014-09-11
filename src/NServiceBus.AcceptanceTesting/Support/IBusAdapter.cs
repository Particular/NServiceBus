namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    public class IBusAdapter : IBus
    {
        readonly ISendOnlyBus sendOnlyBus;

        public IBusAdapter(ISendOnlyBus sendOnlyBus)
        {
            this.sendOnlyBus = sendOnlyBus;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Publish<T>(T message)
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

        public ICallback Send(object message)
        {
            return sendOnlyBus.Send(message);
        }

        public ICallback Send<T>(Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(string destination, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(Address address, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, string> OutgoingHeaders { get; private set; }
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

        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
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

        public IMessageContext CurrentMessageContext { get; private set; }

#pragma warning disable 0618
        public IInMemoryOperations InMemory { get; private set; }
#pragma warning restore 0618
    }
}