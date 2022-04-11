namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Persistence;
    using Pipeline;
    using Unicast;

    class LoadHandlersConnector : StageConnector<IIncomingLogicalMessageContext, IInvokeHandlerContext>
    {
        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry) => this.messageHandlerRegistry = messageHandlerRegistry;

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<IInvokeHandlerContext, Task> stage)
        {
            using var storageSession = context.Builder.GetService<ICompletableSynchronizedStorageSession>() ?? NoOpCompletableSynchronizedStorageSession.Instance;
            await storageSession.OpenSession(context).ConfigureAwait(false);

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

        static readonly ILog logger = LogManager.GetLogger<LoadHandlersConnector>();
        static readonly bool isDebugIsEnabled = logger.IsDebugEnabled;
    }
}