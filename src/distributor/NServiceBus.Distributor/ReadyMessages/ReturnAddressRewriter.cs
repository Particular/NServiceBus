namespace NServiceBus.Distributor.ReadyMessages
{
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast.Transport;

    class ReturnAddressRewriter : IMutateOutgoingTransportMessages
    {
        public Address DistributorDataAddress { get; set; }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            //when not talking to the distributor, pretend that our address is that of the distributor
            if (DistributorDataAddress != null)
                transportMessage.ReplyToAddress = DistributorDataAddress;
        }
    }
}
