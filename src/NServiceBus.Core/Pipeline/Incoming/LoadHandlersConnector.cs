namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;
    using Unicast;

    class LoadHandlersConnector : StageConnector<IIncomingLogicalMessageContext, IInvokeHandlerContext>
    {
        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry, ISynchronizedStorage synchronizedStorage, ISynchronizedStorageAdapter adapter)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
            this.synchronizedStorage = synchronizedStorage;
            this.adapter = adapter;
        }

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<IInvokeHandlerContext, Task> stage)
        {
            var outboxTransaction = context.Extensions.Get<IOutboxTransaction>();
            var transportTransaction = context.Extensions.Get<TransportTransaction>();
            var sessionProvider = context.Builder.GetService<SynchronizedStorageSessionProvider>();
            using (var storageSession = await AdaptOrOpenNewSynchronizedStorageSession(transportTransaction, outboxTransaction, context.Extensions, context.CancellationToken).ConfigureAwait(false))
            {
                if (sessionProvider != null)
                {
                    sessionProvider.SynchronizedStorageSession = storageSession;
                }

                var handlersToInvoke = messageHandlerRegistry.GetHandlersFor(context.Message.MessageType);

                if (!context.MessageHandled && handlersToInvoke.Count == 0)
                {
                    var error = $"No handlers could be found for message type: {context.Message.MessageType}";
                    throw new InvalidOperationException(error);
                }

                if (isDebugIsEnabled)
                {
                    LogHandlersInvocation(context, handlersToInvoke);
                }

                foreach (var messageHandler in handlersToInvoke)
                {
                    messageHandler.Instance = context.Builder.GetRequiredService(messageHandler.HandlerType);

                    var handlingContext = this.CreateInvokeHandlerContext(messageHandler, storageSession, context);
                    await stage(handlingContext).ConfigureAwait(false);

                    if (handlingContext.HandlerInvocationAborted)
                    {
                        //if the chain was aborted skip the other handlers
                        break;
                    }
                }
                context.MessageHandled = true;
                await storageSession.CompleteAsync(context.CancellationToken).ConfigureAwait(false);

                // finally?
                if (sessionProvider != null)
                {
                    sessionProvider.SynchronizedStorageSession = null;
                }
            }
        }

        async Task<ICompletableSynchronizedStorageSession> AdaptOrOpenNewSynchronizedStorageSession(TransportTransaction transportTransaction, IOutboxTransaction outboxTransaction, ContextBag contextBag, CancellationToken cancellationToken)
        {
            return await adapter.TryAdapt(outboxTransaction, contextBag, cancellationToken).ConfigureAwait(false)
                   ?? await adapter.TryAdapt(transportTransaction, contextBag, cancellationToken).ConfigureAwait(false)
                   ?? await synchronizedStorage.OpenSession(contextBag, cancellationToken).ConfigureAwait(false);
        }

        static void LogHandlersInvocation(IIncomingLogicalMessageContext context, List<MessageHandler> handlersToInvoke)
        {
            var builder = new StringBuilder($"Processing message type: {context.Message.MessageType}");
            builder.NewLine("Message headers:");

            foreach (var kvp in context.Headers)
            {
                builder.NewLine($"{kvp.Key} : {kvp.Value}");
            }

            builder.NewLine("Handlers to invoke:");

            foreach (var messageHandler in handlersToInvoke)
            {
                builder.NewLine(messageHandler.HandlerType.FullName);
            }

            logger.Debug(builder.ToString());
        }

        readonly ISynchronizedStorageAdapter adapter;
        readonly ISynchronizedStorage synchronizedStorage;
        readonly MessageHandlerRegistry messageHandlerRegistry;

        static readonly ILog logger = LogManager.GetLogger<LoadHandlersConnector>();
        static readonly bool isDebugIsEnabled = logger.IsDebugEnabled;
    }
}