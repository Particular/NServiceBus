namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast;

    internal class LoadedMessageHandlers : IEnumerable<MessageHandler>
    {
        public IEnumerable<MessageHandler> GetHandlersFor(Type messageType)
        {
            return messageHandlers[messageType];
        }

       
        public void AddHandler(Type messageType, object handler)
        {
            List<MessageHandler> handlersForMessage;

            var loadedHandler = new MessageHandler
            {
                Instance = handler,
                Invocation = (handlerInstance, message) => HandlerInvocationCache.InvokeHandle(handlerInstance, message)
            };

            if (!messageHandlers.TryGetValue(messageType, out handlersForMessage))
            {
                messageHandlers[messageType] = new List<MessageHandler> { loadedHandler };
            }
            else
            {
                handlersForMessage.Add(loadedHandler);
            }
        }


        public IEnumerator<MessageHandler> GetEnumerator()
        {
            return messageHandlers.SelectMany(m => m.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        Dictionary<Type, List<MessageHandler>> messageHandlers = new Dictionary<Type, List<MessageHandler>>();

    
    }

    internal class MessageHandler
    {
        public object Instance { get; set; }
        public Action<object, object> Invocation { get; set; }
        public bool InvocationDisabled { get; set; }
    }

}