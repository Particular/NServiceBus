namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class BusSession : IBusSession
    {
        public BusSession(TransportSendContext context)
        {
            this.context = context;
            this.context.Set(new PendingTransportOperations());
        }

        public Task Send(object message, SendOptions options)
        {
            return BusOperations.Send(context, message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.Send(context, messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return BusOperations.Publish(context, message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions options)
        {
            return BusOperations.Publish(context, messageConstructor, options);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return BusOperations.Subscribe(context, eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.Unsubscribe(context, eventType, options);
        }

        public async Task Dispatch()
        {
            PendingTransportOperations transportOperations;
            if (context.TryGet(out transportOperations))
            {
                if (transportOperations.Operations.Count <= 0)
                {
                    return;
                }

                var batchDispatchContext = new BatchDispatchContext(transportOperations.Operations, context);
                var cache = context.Get<IPipelineCache>();
                var pipeline = cache.Pipeline<IBatchDispatchContext>();
                await pipeline.Invoke(batchDispatchContext).ConfigureAwait(false);
                context.Remove<PendingTransportOperations>();
            }
        }

        public void Dispose()
        {
        }

        void DisposeManaged()
        {
            PendingTransportOperations operations;
            if (context.TryGet(out operations))
            {
                context.Remove<PendingTransportOperations>();
            }
        }
        
        TransportSendContext context;
    }
}