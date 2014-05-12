﻿namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using MessageInterfaces;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LoadHandlersBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        public IMessageMapper MessageMapper { get; set; }

        public PipelineExecutor PipelineFactory { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            var messageToHandle = context.LogicalMessage;

            // for now we cheat and pull it from the behavior context:
            var callbackInvoked = context.Get<bool>(CallbackInvocationBehavior.CallbackInvokedKey);

            var handlerTypedToInvoke = HandlerRegistry.GetHandlerTypes(messageToHandle.MessageType).ToList();

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

                if (PipelineFactory.InvokeHandlerPipeline(loadedHandler).ChainAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }

            next();
        }
    }
}