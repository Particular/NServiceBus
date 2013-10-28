namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Generic;
    using Unicast;

    internal class LoadedMessageHandlers
    {
        public IEnumerable<LoadedHandler> GetHandlersFor(Type messageType)
        {
            return messageHandlers[messageType];
        }

       
        public void AddHandler(Type messageType, object handler)
        {
            List<LoadedHandler> handlersForMessage;

            var loadedHandler = new LoadedHandler
            {
                Instance = handler,
                Invocation = (handlerInstance, message) => HandlerInvocationCache.InvokeHandle(handlerInstance, message)
            };

            if (!messageHandlers.TryGetValue(messageType, out handlersForMessage))
            {
                messageHandlers[messageType] = new List<LoadedHandler> { loadedHandler };
            }
            else
            {
                handlersForMessage.Add(loadedHandler);
            }
        }

        Dictionary<Type, List<LoadedHandler>> messageHandlers = new Dictionary<Type, List<LoadedHandler>>();

        internal class LoadedHandler
        {
            public object Instance{ get; set; }
            public Action<object,object> Invocation { get; set; }
            public bool InvocationDisabled { get; set; }
        }
    }
}