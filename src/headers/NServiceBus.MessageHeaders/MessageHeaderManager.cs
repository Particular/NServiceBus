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
            ExtensionMethods.GetAllHeadersAction = msg => GetAllHeaders(msg);

            Transport.TransportMessageReceived +=
                (s, arg) =>
                {
                    headers = arg.Message.Headers;
                };

            Bus.MessagesSent +=
                (sender, args) =>
                {
                    // clear data for args
                };
        }

        string GetHeader(IMessage message, string key)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                if (headers.ContainsKey(key))
                    return headers[key];
                else
                    return null;

            if (Bus.OutgoingHeaders.ContainsKey(key))
                return Bus.OutgoingHeaders[key];

            return null;
        }

        void SetHeader(IMessage message, string key, string value)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                Bus.CurrentMessageContext.Headers[key] = value;
            else
                Bus.OutgoingHeaders[key] = value;
        }

        IDictionary<string, string> GetAllHeaders(IMessage message)
        {
            return headers;
        }

        [ThreadStatic] private static IDictionary<string, string> headers;
    }
}
