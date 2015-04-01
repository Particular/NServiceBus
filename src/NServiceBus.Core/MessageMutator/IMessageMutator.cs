namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Use this interface to change logical messages before any other code sees them.
    /// </summary>
    public interface IMessageMutator : IMutateOutgoingMessages, IMutateIncomingMessages{}

    /// <summary>
    /// Use this interface to change logical messages before any other code sees them.
    /// </summary>
    /// Daniel: I know this is completely useless trickery. But I want to get going. Andreas is planning to remove that interface anyway
    public interface IMessagesMutator : IMutateOutgoingMessage, IMutateIncomingMessage { }
}