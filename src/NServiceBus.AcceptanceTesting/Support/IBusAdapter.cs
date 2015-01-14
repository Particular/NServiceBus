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
            sendOnlyBus.Dispose();
        }

        public void Publish<T>(T message)
        {
            sendOnlyBus.Publish(message);
        }

        public void Publish<T>()
        {
            sendOnlyBus.Publish<T>();
        }

        public void Publish<T>(Action<T> messageConstructor)
        {
            sendOnlyBus.Publish(messageConstructor);
        }

        public ICallback Send(object message)
        {
            return sendOnlyBus.Send(message);
        }

        public ICallback Send<T>(Action<T> messageConstructor)
        {
            return sendOnlyBus.Send(messageConstructor);
        }

        public ICallback Send(string destination, object message)
        {
            return sendOnlyBus.Send(destination, message);
        }

        public ICallback Send(Address address, object message)
        {
            return sendOnlyBus.Send(address, message);
        }

        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return sendOnlyBus.Send(destination, messageConstructor);
        }

        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            return sendOnlyBus.Send(address, messageConstructor);
        }

        public ICallback Send(string destination, string correlationId, object message)
        {
            return sendOnlyBus.Send(destination, correlationId, message);
        }

        public ICallback Send(Address address, string correlationId, object message)
        {
            return sendOnlyBus.Send(address, correlationId, message);
        }

        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            return sendOnlyBus.Send(destination, correlationId, messageConstructor);
        }

        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            return sendOnlyBus.Send(address, correlationId, messageConstructor);
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