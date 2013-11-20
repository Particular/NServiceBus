namespace NServiceBus.MessageHeaders
{
    using System;
    using System.Collections.Generic;
    using MessageMutator;
    using Pipeline;
    using Unicast;
    using Unicast.Messages;

    /// <summary>
    /// Message Header Manager
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0",TreatAsErrorFromVersion = "5.0")]
    public class MessageHeaderManager : IMutateOutgoingTransportMessages
    {
        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            foreach (var key in staticOutgoingHeaders.Keys)
            {
                transportMessage.Headers[key] = staticOutgoingHeaders[key];
            }

            if (messages != null && messages.Length > 0)
            {
                ApplyMessageSpecificHeaders(messages[0], transportMessage.Headers);
            }
        }

        /// <summary>
        /// Gets the Header for the Message
        /// </summary>
        /// <param name="message">message for which Headers to be find</param>
        /// <param name="key">Key</param>
        public string GetHeader(object message, string key)
        {
            if (message == ExtensionMethods.CurrentMessageBeingHandled)
            {
                LogicalMessage messageBeeingReceived;

                //first try to get the header from the current logical message
                if (PipelineFactory.CurrentContext.TryGet(out messageBeeingReceived))
                {
                    string value;

                    messageBeeingReceived.Headers.TryGetValue(key, out value);

                    return value;
                }

                //falling back to get the headers from the physical message
                // when we remove the multi message feature we can remove this and instead
                // share the same header collection btw physical and logical message
                return TryGetHeaderFromPhysicalMessage(key);
            }

            if (messageHeaders == null)
                return null;

            if (!messageHeaders.ContainsKey(message))
                return null;

            if (messageHeaders[message].ContainsKey(key))
                return messageHeaders[message][key];

            return null;
        }

        string TryGetHeaderFromPhysicalMessage(string key)
        {
            if (bus.CurrentMessageContext != null && bus.CurrentMessageContext.Headers.ContainsKey(key))
            {
                return bus.CurrentMessageContext.Headers[key];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the Header for the Message
        /// </summary>
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

        PipelineFactory PipelineFactory
        {
            get
            {
                //for now, this whole thing will soon be a behavior
                return Configure.Instance.Builder.Build<PipelineFactory>();
            }
        }

        public void ApplyMessageSpecificHeaders(object message, IDictionary<string, string> target)
        {
            if (messageHeaders == null)
            {
                return;
            }
            IDictionary<string, string> source;

            if (!messageHeaders.TryGetValue(message, out source))
            {
                return;
            }

            foreach (var key in source.Keys)
            {
                target[key] = source[key];
            }
        }


        IUnicastBus bus;

        static IDictionary<string, string> staticOutgoingHeaders = new Dictionary<string, string>();

        [ThreadStatic]
        static IDictionary<object, IDictionary<string, string>> messageHeaders;
    }
}
