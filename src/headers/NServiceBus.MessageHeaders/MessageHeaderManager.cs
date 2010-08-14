using System;
using System.Collections.Generic;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.MessageHeaders
{
    public class MessageHeaderManager : IMapOutgoingTransportMessages
    {
        void IMapOutgoingTransportMessages.MapOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            foreach(var key in staticOutgoingHeaders.Keys)
                transportMessage.Headers.Add(key, staticOutgoingHeaders[key]);

            if (messageHeaders != null)
                if (messageHeaders.ContainsKey(messages[0]))
                    foreach(var key in messageHeaders[messages[0]].Keys)
                        transportMessage.Headers.Add(key, messageHeaders[messages[0]][key]);
        }

        public string GetHeader(IMessage message, string key)
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

        public void SetHeader(IMessage message, string key, string value)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                throw new InvalidOperationException("Cannot change headers on the message being processed.");
            
            if (messageHeaders == null)
                messageHeaders = new Dictionary<IMessage, IDictionary<string, string>>();

            if (!messageHeaders.ContainsKey(message))
                messageHeaders.Add(message, new Dictionary<string, string>());

            if (!messageHeaders[message].ContainsKey(key))
                messageHeaders[message].Add(key, value);
            else
                messageHeaders[message][key] = value;
        }

        public IDictionary<string, string> GetStaticOutgoingHeaders()
        {
            return staticOutgoingHeaders;
        }

        public IUnicastBus Bus
        {
            get { return bus; }
            set
            {
                bus = value;
                bus.MessagesSent +=
                            (s2, a2) =>
                            {
                                if (a2.Messages != null)
                                    foreach (var msg in a2.Messages)
                                        messageHeaders.Remove(msg);
                            };
            }
        }
        private IUnicastBus bus;
        
        private static IDictionary<string, string> staticOutgoingHeaders = new Dictionary<string, string>();

        [ThreadStatic] private static IDictionary<IMessage, IDictionary<string, string>> messageHeaders;
    }
}
