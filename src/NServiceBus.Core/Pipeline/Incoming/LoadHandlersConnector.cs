namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using ObjectBuilder;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;
    using Unicast;

    class LoadHandlersConnector : StageConnector<IIncomingLogicalMessageContext, IInvokeHandlerContext>
    {
        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry, ISynchronizedStorage synchronizedStorage, ISynchronizedStorageAdapter adapter, IBuilder serviceProvider, bool useScopedSession)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
            this.synchronizedStorage = synchronizedStorage;
            this.adapter = adapter;
            this.serviceProvider = serviceProvider;
            this.useScopedSession = useScopedSession;
        }

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<IInvokeHandlerContext, Task> stage)
        {
            var outboxTransaction = context.Extensions.Get<OutboxTransaction>();
            var transportTransaction = context.Extensions.Get<TransportTransaction>();

            if (useScopedSession)
            {
                var scopedSession = serviceProvider.Build<ICompletableSynchronizedStorageSession>();

                await scopedSession.OpenSession(outboxTransaction, transportTransaction, context.Extensions).ConfigureAwait(false);
                await InvokeHandlers(context, stage, scopedSession).ConfigureAwait(false);
                await scopedSession.CompleteAsync().ConfigureAwait(false);
            }
            else
            {
                using (var storageSession = await AdaptOrOpenNewSynchronizedStorageSession(transportTransaction, outboxTransaction, context.Extensions).ConfigureAwait(false))
                {
                    await InvokeHandlers(context, stage, storageSession).ConfigureAwait(false);
                    await storageSession.CompleteAsync().ConfigureAwait(false);
                }
            }
        }

        async Task InvokeHandlers(IIncomingLogicalMessageContext context, Func<IInvokeHandlerContext, Task> stage, SynchronizedStorageSession storageSession)
        {
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
                messageHandler.Instance = context.Builder.Build(messageHandler.HandlerType);

                var handlingContext = this.CreateInvokeHandlerContext(messageHandler, storageSession, context);
                await stage(handlingContext).ConfigureAwait(false);

                if (handlingContext.HandlerInvocationAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }

            context.MessageHandled = true;
        }

        async Task<CompletableSynchronizedStorageSession> AdaptOrOpenNewSynchronizedStorageSession(TransportTransaction transportTransaction, OutboxTransaction outboxTransaction, ContextBag contextBag)
        {
            return await adapter.TryAdapt(outboxTransaction, contextBag).ConfigureAwait(false)
                   ?? await adapter.TryAdapt(transportTransaction, contextBag).ConfigureAwait(false)
                   ?? await synchronizedStorage.OpenSession(contextBag).ConfigureAwait(false);
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

        readonly MessageHandlerRegistry messageHandlerRegistry;
        readonly IBuilder serviceProvider;
        readonly bool useScopedSession;
        readonly ISynchronizedStorage synchronizedStorage;
        readonly ISynchronizedStorageAdapter adapter;

        static readonly ILog logger = LogManager.GetLogger<LoadHandlersConnector>();
        static readonly bool isDebugIsEnabled = logger.IsDebugEnabled;

        [Janitor.SkipWeaving]
        class CompletableWrapper : IDisposable
        {
            readonly Func<Task> completeCallback;
            readonly Action disposeCallback;

            public CompletableWrapper(Func<Task> completeCallback, Action disposeCallback)
            {
                this.completeCallback = completeCallback;
                this.disposeCallback = disposeCallback;
            }

            public Task Complete() => completeCallback();

            public void Dispose() => disposeCallback();
        }
    }
}