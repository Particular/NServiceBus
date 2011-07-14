using NServiceBus.Grid.Messages;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Distributor
{
    class ReturnAddressRewriter : IMutateOutgoingTransportMessages
    {
        public Address DistributorDataQueue { get; set; }

        public void MutateOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            if (messages[0] is ReadyMessage)
                return;

            //when not talking to the distributor, pretend that our address is that of the distributor
            if (DistributorDataQueue != null)
                transportMessage.ReplyToAddress = DistributorDataQueue;
        }
    }
}
