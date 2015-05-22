namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Behaviors;
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
            var handlerTypedToInvoke = messageHandlerRegistry.GetHandlerTypes(context.MessageType).ToList();

            if (!context.MessageHandled && !handlerTypedToInvoke.Any())
            {
                var error = string.Format("No handlers could be found for message type: {0}", context.MessageType);
                throw new InvalidOperationException(error);
            }

            foreach (var handlerType in handlerTypedToInvoke)
            {
                var loadedHandler = new MessageHandler
                {
                    Instance = context.Builder.Build(handlerType),
                    Invocation = (handlerInstance, message) => messageHandlerRegistry.InvokeHandle(handlerInstance, message)
                };

                var handlingContext = new HandlingStageBehavior.Context(loadedHandler, context);
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