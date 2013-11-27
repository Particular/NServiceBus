namespace NServiceBus.Distributor.ReadyMessages
{
    using MessageMutator;

    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]    
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
