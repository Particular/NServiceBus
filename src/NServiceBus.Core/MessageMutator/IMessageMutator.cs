namespace NServiceBus.MessageMutator
{
    /// <summary>
    /// Use this interface to change logical messages before any other code sees them.
    /// </summary>
    public interface IMessageMutator : IMutateOutgoingMessages, IMutateIncomingMessages{}
}