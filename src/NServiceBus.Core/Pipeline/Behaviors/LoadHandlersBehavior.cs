namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Linq;
    using MessageInterfaces;
    using Unicast;

    class LoadHandlersBehavior : IBehavior<LogicalMessageContext>
    {
        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        public IMessageMapper MessageMapper { get; set; }

        public void Invoke(LogicalMessageContext context, Action next)
        {
            var messageToHandle = context.LogicalMessage;

            // for now we cheat and pull it from the behavior context:
            var callbackInvoked = context.Get<bool>(CallbackInvocationBehavior.CallbackInvokedKey);
            var messageHandlers = new LoadedMessageHandlers();

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


            //todo: remove this
            context.Set(messageHandlers);

            foreach (var handler in messageHandlers)
            {
                context.PipelineFactory.InvokeHandlerPipeline(handler);
            }

            next();
        }
    }
}