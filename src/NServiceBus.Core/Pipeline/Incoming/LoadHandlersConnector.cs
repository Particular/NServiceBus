namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class LoadHandlersConnector : StageConnector<LogicalMessageProcessingContext, InvokeHandlerContext>
    {
        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry, ISynchronizedStorage synchronizedStorage, ISynchronizedStorageAdapter adapter)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
            this.synchronizedStorage = synchronizedStorage;
            this.adapter = adapter;
        }

        public override async Task Invoke(LogicalMessageProcessingContext context, Func<InvokeHandlerContext, Task> next)
        {
            var outboxTransaction = context.Get<OutboxTransaction>();
            var transportTransaction = context.Get<TransportTransaction>();
            using (var storageSession = await AdaptOrOpenNewSynchronizedStorageSession(transportTransaction, outboxTransaction, context))
            {
                var handlersToInvoke = messageHandlerRegistry.GetHandlersFor(context.Message.MessageType).ToList();

                if (!context.MessageHandled && !handlersToInvoke.Any())
                {
                    var error = $"No handlers could be found for message type: {context.Message.MessageType}";
                    throw new InvalidOperationException(error);
                }

                foreach (var messageHandler in handlersToInvoke)
                {
                    messageHandler.Instance = context.Builder.Build(messageHandler.HandlerType);

                    var handlingContext = new InvokeHandlerContext(messageHandler, storageSession, context);
                    await next(handlingContext).ConfigureAwait(false);

                    if (handlingContext.HandlerInvocationAborted)
                    {
                        //if the chain was aborted skip the other handlers
                        break;
                    }
                }
                context.MessageHandled = true;
                await storageSession.CompleteAsync().ConfigureAwait(false);
            }
        }

        Task<ICompletableSynchronizedStorageSession> AdaptOrOpenNewSynchronizedStorageSession(TransportTransaction transportTransaction, OutboxTransaction outboxTransaction, ContextBag contextBag)
        {
            ICompletableSynchronizedStorageSession session;
            return adapter.TryAdapt(transportTransaction, out session) || adapter.TryAdapt(outboxTransaction, out session)
                ? Task.FromResult(session)
                : synchronizedStorage.OpenSession(contextBag);
        }


        MessageHandlerRegistry messageHandlerRegistry;
        readonly ISynchronizedStorage synchronizedStorage;
        readonly ISynchronizedStorageAdapter adapter;
    }
}