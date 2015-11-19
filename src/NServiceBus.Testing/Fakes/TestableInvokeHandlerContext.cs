namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;

    public class TestableInvokeHandlerContext : InvokeHandlerContext
    {
        public BusOperations BusOperations { get; } = new BusOperations();

        public bool HandleCurrentMessageLaterCalled { get; private set; }

        public Task HandleCurrentMessageLaterAsync()
        {
            HandleCurrentMessageLaterCalled = true;
            return Task.CompletedTask;
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            //TODO
        }

        public ContextBag Extensions { get; }

        public Task SendAsync(object message, SendOptions options)
        {
            return BusOperations.SendAsync(message, options);
        }

        public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.SendAsync(messageConstructor, options);
        }

        public Task PublishAsync(object message, PublishOptions options)
        {
            return BusOperations.PublishAsync(message, options);
        }

        public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperations.PublishAsync(messageConstructor, publishOptions);
        }

        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return BusOperations.SubscribeAsync(eventType, options);
        }

        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.UnsubscribeAsync(eventType, options);
        }

        public string MessageId { get; }
        public string ReplyToAddress { get; }
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        public Task ReplyAsync(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task ForwardCurrentMessageToAsync(string destination)
        {
            throw new NotImplementedException();
        }

        public MessageHandler MessageHandler { get; }

        public Dictionary<string, string> Headers { get; set; }

        public object MessageBeingHandled { get; set; }

        public bool HandlerInvocationAborted { get; }

        public MessageMetadata MessageMetadata { get; }

        public IBuilder Builder { get; set; }
        public T Get<T>()
        {
            throw new NotImplementedException();
        }

        public bool TryGet<T>(out T result)
        {
            throw new NotImplementedException();
        }

        public bool TryGet<T>(string key, out T result)
        {
            throw new NotImplementedException();
        }

        public T GetOrCreate<T>() where T : class, new()
        {
            throw new NotImplementedException();
        }

        public void Set<T>(T t)
        {
            throw new NotImplementedException();
        }

        public void Remove<T>()
        {
            throw new NotImplementedException();
        }

        public void Set<T>(string key, T t)
        {
            throw new NotImplementedException();
        }

        public bool TryGetRootContext<T>(out T result) where T : class
        {
            throw new NotImplementedException();
        }

        public void Merge(ContextBag context)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, object> GetAll()
        {
            throw new NotImplementedException();
        }

        public PipelineInfo PipelineInfo { get; }
    }
}