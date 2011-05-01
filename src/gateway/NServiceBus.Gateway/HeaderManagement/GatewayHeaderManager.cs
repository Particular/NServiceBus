namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Collections.Generic;
    using MessageMutator;
    using Unicast.Transport;

    public class GatewayHeaderManager : IMutateIncomingMessages, IMutateOutgoingTransportMessages
    {
        public IBus Bus { get; set; }

        public IMessage MutateIncoming(IMessage message)
        {
            var originatingSite = message.GetOriginatingSiteHeader();

            if (originatingSite != null)
            {
                var returnAddress = message.GetHeader("ReturnAddress");
                var id = Bus.CurrentMessageContext.Id;

                if (messageReturns == null)
                    messageReturns = new Dictionary<string, GatewayReturnInfo>();

                messageReturns.Add(id, new GatewayReturnInfo
                                           {
                                               From = originatingSite, 
                                               ReturnAddress = returnAddress
                                           });
            }

            return message;
        }

        public void MutateOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            if (messageReturns == null)
                return;

            GatewayReturnInfo info;

            if (!messageReturns.TryGetValue(transportMessage.CorrelationId, out info)) 
                return;

            if (transportMessage.Headers.ContainsKey(Headers.DestinationSites)) 
                return;
            
            transportMessage.Headers[Headers.DestinationSites] = info.From;

            if (!transportMessage.Headers.ContainsKey(Headers.RouteTo))
                transportMessage.Headers[Headers.RouteTo] = info.ReturnAddress;
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