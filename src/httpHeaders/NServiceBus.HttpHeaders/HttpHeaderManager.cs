using System;
using System.Collections.Generic;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.HttpHeaders
{
    class HttpHeaderManager : IMutateIncomingMessages, IMutateOutgoingTransportMessages
    {
        public IBus Bus { get; set; }

        public IMessage MutateIncoming(IMessage message)
        {
            var httpFrom = message.GetHttpFromHeader();
            if (httpFrom != null)
            {
                var httpTo = message.GetHttpToHeader();
                var returnAddress = message.GetHeader("ReturnAddress");
                var id = Bus.CurrentMessageContext.Id;

                if (messageReturns == null)
                    messageReturns = new Dictionary<string, HttpReturnInfo>();

                messageReturns.Add(id, new HttpReturnInfo { From = httpFrom, To = httpTo, ReturnAddress = returnAddress });
            }

            return message;
        }

        public void MutateOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            if (messageReturns == null)
                return;

            HttpReturnInfo info = null;
            if (messageReturns.TryGetValue(transportMessage.CorrelationId, out info))
            {
                if (!transportMessage.Headers.ContainsKey(Headers.HttpTo))
                {
                    transportMessage.Headers[Headers.HttpTo] = info.From;

                    if (!transportMessage.Headers.ContainsKey(Headers.RouteTo))
                        transportMessage.Headers[Headers.RouteTo] = info.ReturnAddress;
                }
            }
        }

        public ITransport Transport
        {
            get { return transport; }
            set
            {
                transport = value;
                transport.FinishedMessageProcessing +=
                    (s, e) =>
                        {
                            messageReturns = null;
                        };
            }
        }
        private ITransport transport;

        [ThreadStatic] private IDictionary<string, HttpReturnInfo> messageReturns;
    }

    class HttpReturnInfo
    {
        public string From { get; set; }
        public string To { get; set; }
        public string ReturnAddress { get; set; }
    }

    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            NServiceBus.Configure.Instance.Configurer.ConfigureComponent<HttpHeaderManager>(
                DependencyLifecycle.SingleInstance);
        }
    }
}
