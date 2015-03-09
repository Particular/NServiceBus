namespace NServiceBus
{
    using System;
    using System.Linq;
    using NServiceBus.Unicast.Behaviors;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class LoadHandlersConnector : StageConnector<LogicalMessageProcessingStageBehavior.Context, HandlingStageBehavior.Context>
    {
        readonly MessageHandlerRegistry messageHandlerRegistry;

        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
        }

        public override void Invoke(LogicalMessageProcessingStageBehavior.Context context, Action<HandlingStageBehavior.Context> next)
        {
            var messageToHandle = context.IncomingLogicalMessage;

            bool callbackInvoked;

            // for now we cheat and pull it from the behavior context:
            if (!context.TryGet(CallbackInvocationBehavior.CallbackInvokedKey, out callbackInvoked))
            {
                callbackInvoked = false;
            }

            var handlerTypedToInvoke = messageHandlerRegistry.GetHandlersFor(messageToHandle.MessageType).ToList();

            if (!callbackInvoked && !handlerTypedToInvoke.Any())
            {
                var error = string.Format("No handlers could be found for message type: {0}", messageToHandle.MessageType);
                throw new InvalidOperationException(error);
            }

            foreach (var loadedHandler in handlerTypedToInvoke)
            {
                loadedHandler.Instance = context.Builder.Build(loadedHandler.HandlerType);

                var handlingContext = new HandlingStageBehavior.Context(loadedHandler, context);
                next(handlingContext);

                if (handlingContext.HandlerInvocationAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }
        }
    }
}