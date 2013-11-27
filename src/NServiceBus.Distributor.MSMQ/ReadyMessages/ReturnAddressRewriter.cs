namespace NServiceBus.Distributor.MSMQ.ReadyMessages
{
    using MessageMutator;

    internal class ReturnAddressRewriter : IMutateOutgoingTransportMessages
    {
        public Address DistributorDataAddress { get; set; }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            //when not talking to the distributor, pretend that our address is that of the distributor
            if (DistributorDataAddress != null)
            {
                transportMessage.ReplyToAddress = DistributorDataAddress;
            }
        }
    }
}