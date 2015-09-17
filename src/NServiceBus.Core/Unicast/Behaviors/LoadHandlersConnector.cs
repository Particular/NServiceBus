namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class LoadHandlersConnector : StageConnector<LogicalMessageProcessingStageBehavior.Context, HandlingStageBehavior.Context>
    {
        MessageHandlerRegistry messageHandlerRegistry;

        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
        }

        public override async Task Invoke(LogicalMessageProcessingStageBehavior.Context context, Func<HandlingStageBehavior.Context, Task> next)
        {
            var handlersToInvoke = messageHandlerRegistry.GetHandlersFor(context.MessageType).ToList();

            if (!context.MessageHandled && !handlersToInvoke.Any())
            {
                var error = string.Format("No handlers could be found for message type: {0}", context.MessageType);
                throw new InvalidOperationException(error);
            }

            foreach (var messageHandler in handlersToInvoke)
            {
                messageHandler.Instance = context.Builder.Build(messageHandler.HandlerType);

                var handlingContext = new HandlingStageBehavior.Context(messageHandler, context);
                await next(handlingContext).ConfigureAwait(false);

                if (handlingContext.HandlerInvocationAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }

            context.MessageHandled = true;
        }
    }
}