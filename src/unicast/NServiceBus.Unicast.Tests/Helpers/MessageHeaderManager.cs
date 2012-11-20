namespace NServiceBus.Unicast.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using MessageMutator;

    public class MessageHeaderManager : IMutateOutgoingTransportMessages
    {
        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            foreach (var staticHeader in staticHeaders.Keys)
            {
                transportMessage.Headers.Add(staticHeader, staticHeaders[staticHeader]);
            }

            if (messageHeaders == null)
                return;

            if ((messages != null) && (messages.Length > 0) && (messageHeaders.ContainsKey(messages[0])))
                foreach (var key in messageHeaders[messages[0]].Keys)
                    transportMessage.Headers.Add(key, messageHeaders[messages[0]][key]);

            messageHeaders.Clear();
        }


        public void SetHeader(object message, string key, string value)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
                throw new InvalidOperationException("Cannot change headers on the message being processed.");

            if (messageHeaders == null)
                messageHeaders = new Dictionary<object, IDictionary<string, string>>();

            if (!messageHeaders.ContainsKey(message))
                messageHeaders.Add(message, new Dictionary<string, string>());

            if (!messageHeaders[message].ContainsKey(key))
                messageHeaders[message].Add(key, value);
            else
                messageHeaders[message][key] = value;
        }

        public static IDictionary<string, string> staticHeaders = new Dictionary<string, string>();
        static IDictionary<object, IDictionary<string, string>> messageHeaders;
    }
}