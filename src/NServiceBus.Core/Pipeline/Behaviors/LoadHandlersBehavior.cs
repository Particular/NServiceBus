namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Linq;
    using ObjectBuilder;
    using Unicast;

    class LoadHandlersBehavior : IBehavior
    {
        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        public IBuilder Builder { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            var messagesToLoadHandlersFor = context.Messages;

            if (context.Messages == null)
            {
                var error = string.Format("Messages has not been set on the current behavior context: {0} - DispatchToHandlers must be executed AFTER having extracted the messages", context);
                throw new ArgumentException(error);
            }

            // for now we cheat and pull it from the behavior context:
            var callbackInvoked = context.Get<bool>(CallbackInvocationBehavior.CallbackInvokedKey);
            var messageHandlers = new LoadedMessageHandlers();

            foreach (var messageToHandle in messagesToLoadHandlersFor)
            {
                var messageType = messageToHandle.GetType();

                var handlerTypedToInvoke = HandlerRegistry.GetHandlerTypes(messageType).ToList();

                if (!callbackInvoked && !handlerTypedToInvoke.Any())
                {
                    var error = string.Format("No handlers could be found for message type: {0}", messageToHandle.GetType().FullName);
                    throw new InvalidOperationException(error);
                }

                foreach (var type in handlerTypedToInvoke)
                {
                    messageHandlers.AddHandler(messageType, Builder.Build(type));
                }
            }
            context.Set(messageHandlers);

            next();
        }
    }
}