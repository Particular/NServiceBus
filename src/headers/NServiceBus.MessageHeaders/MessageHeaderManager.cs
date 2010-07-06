using System;
using System.Collections.Generic;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.MessageHeaders
{
    class MessageHeaderManager : INeedInitialization, IMapOutgoingTransportMessages
    {
        public IUnicastBus Bus { get; set; }
        public ITransport Transport { get; set; }

        void INeedInitialization.Init()
        {
            ExtensionMethods.GetHeaderAction = (msg, key) => GetHeader(msg, key);
            ExtensionMethods.SetHeaderAction = (msg, key, val) => SetHeader(msg, key, val);
            ExtensionMethods.GetStaticOutgoingHeadersAction = () => GetStaticOutgoingHeaders();

            Transport.TransportMessageReceived +=
                (s, arg) =>
                {
                    staticOutgoingHeaders.Clear();
                };

            Bus.MessagesSent +=
                (sender, args) =>
                {
                    if (args.Messages != null)
                        foreach (var msg in args.Messages)
                            messageHeaders.Remove(msg);
                };
        }

        void IMapOutgoingTransportMessages.MapOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            foreach(var key in staticOutgoingHeaders.Keys)
                transportMessage.Headers.Add(key, staticOutgoingHeaders[key]);

            if (messageHeaders != null)
                if (messageHeaders.ContainsKey(messages[0]))
                    foreach(var key in messageHeaders[messages[0]].Keys)
                        transportMessage.Headers.Add(key, messageHeaders[messages[0]][key]);
        }

        string GetHeader(IMessage message, string key)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                if (Bus.CurrentMessageContext.Headers.ContainsKey(key))
                    return Bus.CurrentMessageContext.Headers[key];
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

        void SetHeader(IMessage message, string key, string value)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                throw new InvalidOperationException("Cannot change headers on the message being processed.");
            else
            {
                if (messageHeaders == null)
                    messageHeaders = new Dictionary<IMessage, IDictionary<string, string>>();

                if (!messageHeaders.ContainsKey(message))
                    messageHeaders.Add(message, new Dictionary<string, string>());

                if (!messageHeaders[message].ContainsKey(key))
                    messageHeaders[message].Add(key, value);
                else
                    messageHeaders[message][key] = value;
            }
        }

        IDictionary<string, string> GetStaticOutgoingHeaders()
        {
            return staticOutgoingHeaders;
        }

        private static IDictionary<string, string> staticOutgoingHeaders = new Dictionary<string, string>();

        [ThreadStatic] private static IDictionary<IMessage, IDictionary<string, string>> messageHeaders;
    }
}
