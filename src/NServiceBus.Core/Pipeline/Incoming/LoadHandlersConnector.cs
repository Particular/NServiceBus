namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class LoadHandlersConnector : StageConnector<LogicalMessageProcessingContext, InvokeHandlerContext>
    {
        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
        }

        public override async Task Invoke(LogicalMessageProcessingContext context, Func<InvokeHandlerContext, Task> next)
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

                var handlingContext = new InvokeHandlerContext(messageHandler, context);
                await next(handlingContext).ConfigureAwait(false);

                if (handlingContext.HandlerInvocationAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }

            context.MessageHandled = true;
        }

        MessageHandlerRegistry messageHandlerRegistry;
    }
}