namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    public class TestableOutgoingLogicalMessageContext : OutgoingLogicalMessageContext
    {
        // TODO Testing: quick hack:
        object rootContext;
        public void SetRootContext<T>(T context)
        {
            rootContext = context;
        }

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
            var ctx = rootContext as T;
            result = ctx;
            return ctx != null;
        }

        public void Merge(ContextBag context)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, object> GetAll()
        {
            throw new NotImplementedException();
        }

        public IBuilder Builder { get; }
        public ContextBag Extensions { get; }
        public Task SendAsync(object message, SendOptions options)
        {
            throw new NotImplementedException();
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

        public string MessageId { get; }
        public Dictionary<string, string> Headers { get; }
        public OutgoingLogicalMessage Message { get; }
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }
        public void UpdateMessageInstance(object newInstance)
        {
            throw new NotImplementedException();
        }
    }
}