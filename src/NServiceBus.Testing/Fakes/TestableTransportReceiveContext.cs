namespace NServiceBus.Testing.Fakes
{
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    public class TestableTransportReceiveContext : TransportReceiveContext
    {
        public T Get<T>()
        {
            throw new System.NotImplementedException();
        }

        public bool TryGet<T>(out T result)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGet<T>(string key, out T result)
        {
            throw new System.NotImplementedException();
        }

        public T GetOrCreate<T>() where T : class, new()
        {
            throw new System.NotImplementedException();
        }

        public void Set<T>(T t)
        {
            throw new System.NotImplementedException();
        }

        public void Remove<T>()
        {
            throw new System.NotImplementedException();
        }

        public void Set<T>(string key, T t)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetRootContext<T>(out T result) where T : class
        {
            throw new System.NotImplementedException();
        }

        public void Merge(ContextBag context)
        {
            throw new System.NotImplementedException();
        }

        public IDictionary<string, object> GetAll()
        {
            throw new System.NotImplementedException();
        }

        public IBuilder Builder { get; }
        public IncomingMessage Message { get; }
        public PipelineInfo PipelineInfo { get; }
    }
}