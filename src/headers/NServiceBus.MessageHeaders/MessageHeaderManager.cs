using System;
using System.Collections.Generic;
using NServiceBus.Config;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.MessageHeaders
{
    class MessageHeaderManager : INeedInitialization
    {
        public IUnicastBus Bus { get; set; }
        public ITransport Transport { get; set; }

        void INeedInitialization.Init()
        {
            ExtensionMethods.GetHeaderAction = (msg, key) => GetHeader(msg, key);
            ExtensionMethods.SetHeaderAction = (msg, key, val) => SetHeader(msg, key, val);
            ExtensionMethods.GetStaticOutgoingHeadersAction = () => GetStaticOugoingHeaders();

            Transport.TransportMessageReceived +=
                (s, arg) =>
                {
                    headersOfMessageBeingProcessed = arg.Message.Headers;
                    staticOutgoingHeaders = new Dictionary<string, string>();
                };

            Bus.MessagesSent +=
                (sender, args) =>
                {
                    if (args.Messages != null)
                        foreach (var msg in args.Messages)
                            messageHeaders.Remove(msg);
                };
        }

        string GetHeader(IMessage message, string key)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                if (headersOfMessageBeingProcessed.ContainsKey(key))
                    return headersOfMessageBeingProcessed[key];
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

        IDictionary<string, string> GetStaticOugoingHeaders()
        {
            return staticOutgoingHeaders;
        }

        [ThreadStatic] private static IDictionary<string, string> headersOfMessageBeingProcessed;
        [ThreadStatic] private static IDictionary<string, string> staticOutgoingHeaders;

        [ThreadStatic] private static IDictionary<IMessage, IDictionary<string, string>> messageHeaders;
    }
}
