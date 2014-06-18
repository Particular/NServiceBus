namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Use this interface to change transport messages before any other code sees them. 
    /// </summary>
    public interface IMutateTransportMessages : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages {}
}
