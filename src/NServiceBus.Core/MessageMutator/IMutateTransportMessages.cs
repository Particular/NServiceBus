namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Use this interface to change transport messages before any other code sees them. 
    /// </summary>
    public interface IMutateTransportMessages : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages {}

    /// <summary>
    /// Use this interface to change transport messages before any other code sees them. 
    /// </summary>
    /// Daniel: We need a better name
    public interface IMutateTransportMessage : IMutateIncomingTransportMessage, IMutateOutgoingTransportMessage { }
}
