namespace NServiceBus.Saga
{
    /// <summary>
    /// Marker interface for timeout state messages
    /// </summary>
    public interface ITimeoutState : IMessage
    {
    }
}