namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Collections.Generic;
    using MessageMutator;
    using Unicast.Transport;

    public class GatewayHeaderManager : IMutateIncomingMessages, IMutateOutgoingTransportMessages
    {
        public IBus Bus { get; set; }

        public object MutateIncoming(object message)
        {
            var originatingSite = message.GetOriginatingSiteHeader();

            if (originatingSite != null)
            {
                if (messageReturns == null)
                    messageReturns = new Dictionary<string, GatewayReturnInfo>();

                messageReturns.Add(Bus.CurrentMessageContext.Id, 
                                    new GatewayReturnInfo
                                           {
                                               From = originatingSite,
                                               ReplyToAddress = Bus.CurrentMessageContext.ReplyToAddress.ToString()
                                           });
            }

            return message;
        }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (messageReturns == null)
                return;

            GatewayReturnInfo info;

            if (transportMessage.CorrelationId == null)
                return;

            if (!messageReturns.TryGetValue(transportMessage.CorrelationId, out info)) 
                return;

            if (transportMessage.Headers.ContainsKey(Headers.DestinationSites)) 
                return;
            
            transportMessage.Headers[Headers.DestinationSites] = info.From;

            if (!transportMessage.Headers.ContainsKey(Headers.RouteTo))
                transportMessage.Headers[Headers.RouteTo] = info.ReplyToAddress;
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
        
        ITransport transport;

        [ThreadStatic]
        private IDictionary<string, GatewayReturnInfo> messageReturns;
    }
}