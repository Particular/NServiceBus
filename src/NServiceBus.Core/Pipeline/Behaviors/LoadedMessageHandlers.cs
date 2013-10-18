namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Generic;

    internal class LoadedMessageHandlers
    {
        public IEnumerable<object> GetHandlersFor(Type messageType)
        {
            return messageHandlers[messageType];
        }

        Dictionary<Type, List<object>> messageHandlers = new Dictionary<Type, List<object>>();

        public void AddHandler(Type messageType, object handler)
        {
            List<object> handlersForMessage;

            if (!messageHandlers.TryGetValue(messageType, out handlersForMessage))
            {
                messageHandlers[messageType] = new List<object>{handler};
            }
            else
            {
                handlersForMessage.Add(handler);
            }
        }
    }
}