namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Linq;
    using MessageInterfaces;
    using Unicast;

    class LoadHandlersBehavior : IBehavior<PhysicalMessageContext>
    {
        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        public IMessageMapper MessageMapper { get; set; }

        public void Invoke(PhysicalMessageContext context, Action next)
        {
            // for now we cheat and pull it from the behavior context:
            var callbackInvoked = context.Get<bool>(CallbackInvocationBehavior.CallbackInvokedKey);
            var messageHandlers = new LoadedMessageHandlers();

            foreach (var messageToHandle in context.Get<LogicalMessages>())
            {
                var handlerTypedToInvoke = HandlerRegistry.GetHandlerTypes(messageToHandle.MessageType).ToList();

                if (!callbackInvoked && !handlerTypedToInvoke.Any())
                {
                    var error = string.Format("No handlers could be found for message type: {0}", messageToHandle.MessageType);
                    throw new InvalidOperationException(error);
                }

                foreach (var handlerType in handlerTypedToInvoke)
                {
                    messageHandlers.AddHandler(messageToHandle.MessageType, context.Builder.Build(handlerType));
                }
            }
         
            context.Set(messageHandlers);

            foreach (var handler in messageHandlers)
            {
                var handlerPipeline = context.PipelineFactory.GetHandlerPipeline(handler);

                handlerPipeline();
            }

            next();
        }
    }
}