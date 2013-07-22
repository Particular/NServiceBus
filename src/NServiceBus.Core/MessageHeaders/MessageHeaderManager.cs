namespace NServiceBus.MessageHeaders
{
    using System;
    using System.Collections.Generic;
    using MessageMutator;
    using Unicast;

    /// <summary>
    /// Message Header Manager
    /// </summary>
    public class MessageHeaderManager : IMutateOutgoingTransportMessages
    {
        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            foreach (var key in staticOutgoingHeaders.Keys)
            {
                transportMessage.Headers.Add(key, staticOutgoingHeaders[key]);
            }

            if ((messages != null) && (messages.Length > 0) && (messageHeaders != null))
            {
                if (messageHeaders.ContainsKey(messages[0]))
                {
                    foreach (var key in messageHeaders[messages[0]].Keys)
                    {
                        transportMessage.Headers[key] = messageHeaders[messages[0]][key];
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Header for the Message
        /// </summary>
        /// <param name="message">message for which Headers to be find</param>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public string GetHeader(object message, string key)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                if (bus.CurrentMessageContext.Headers.ContainsKey(key))
                    return bus.CurrentMessageContext.Headers[key];
                else
                    return null;

            if (messageHeaders == null)
                return null;

            if (!messageHeaders.ContainsKey(message))
                return null;

            if (messageHeaders[message].ContainsKey(key))
                return messageHeaders[message][key];

            return null;
        }

        /// <summary>
        /// Sets the Header for the Message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetHeader(object message, string key, string value)
        {
            if (message == null)
                throw new InvalidOperationException("Cannot set headers on a null object");
         
            if (messageHeaders == null)
                messageHeaders = new Dictionary<object, IDictionary<string, string>>();

            if (!messageHeaders.ContainsKey(message))
                messageHeaders.Add(message, new Dictionary<string, string>());

            if (!messageHeaders[message].ContainsKey(key))
                messageHeaders[message].Add(key, value);
            else
                messageHeaders[message][key] = value;
        }

        /// <summary>
        /// Gets Static Outgoing Headers
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetStaticOutgoingHeaders()
        {
            return staticOutgoingHeaders;
        }

        /// <summary>
        /// Bus
        /// </summary>
        public IUnicastBus Bus
        {
            get { return bus; }
            set
            {
                bus = value;
                bus.MessagesSent +=
                            (s2, a2) =>
                            {
                                if (a2.Messages != null && messageHeaders != null)
                                    foreach (var msg in a2.Messages)
                                        messageHeaders.Remove(msg);
                            };
            }
        }
        private IUnicastBus bus;
        
        private static IDictionary<string, string> staticOutgoingHeaders = new Dictionary<string, string>();

        [ThreadStatic]
        private static IDictionary<object, IDictionary<string, string>> messageHeaders;
    }
}
