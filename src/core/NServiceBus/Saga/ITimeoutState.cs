namespace NServiceBus.Saga
{
    /// <summary>
    /// Marker interface for timeout state messages
    /// </summary>
    [ObsoleteEx(Message = "Timeouts no longer need to inherit from ITimeoutState so this interface can safely be removed", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface ITimeoutState : IMessage
    {
    }
}