using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Distributor
{
    class ReturnAddressRewriter : IMutateOutgoingTransportMessages
    {
        public Address DistributorDataQueue { get; set; }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            //when not talking to the distributor, pretend that our address is that of the distributor
            if (DistributorDataQueue != null)
                transportMessage.ReplyToAddress = DistributorDataQueue;
        }
    }
}
