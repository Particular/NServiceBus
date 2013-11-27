namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LoadHandlersBehavior : IBehavior<ReceiveLogicalMessageContext>
    {
        IMessageHandlerRegistry handlerRegistry;
        PipelineFactory pipelineFactory;

        internal LoadHandlersBehavior(IMessageHandlerRegistry handlerRegistry, PipelineFactory pipelineFactory)
        {
            this.handlerRegistry = handlerRegistry;
            this.pipelineFactory = pipelineFactory;
        }


        public void Invoke(ReceiveLogicalMessageContext context, Action next)
        {
            var messageToHandle = context.LogicalMessage;

            // for now we cheat and pull it from the behavior context:
            var callbackInvoked = context.Get<bool>(CallbackInvocationBehavior.CallbackInvokedKey);

            var handlerTypedToInvoke = handlerRegistry.GetHandlerTypes(messageToHandle.MessageType).ToList();

            if (!callbackInvoked && !handlerTypedToInvoke.Any())
            {
                var error = string.Format("No handlers could be found for message type: {0}", messageToHandle.MessageType);
                throw new InvalidOperationException(error);
            }

            foreach (var handlerType in handlerTypedToInvoke)
            {
                var loadedHandler = new MessageHandler
                {
                    Instance = context.Builder.Build(handlerType),
                    Invocation = (handlerInstance, message) => HandlerInvocationCache.InvokeHandle(handlerInstance, message)
                };

                if (pipelineFactory.InvokeHandlerPipeline(loadedHandler).ChainAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }

            next();
        }
    }
}