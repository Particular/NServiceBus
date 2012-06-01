namespace NServiceBus.Saga
{
    using System;

    /// <summary>
    /// Marker interface for timeout state messages
    /// </summary>
    [Obsolete("Timeouts no longer need to inherit from ITimeoutState so this interface can safely be removed",false)]
    public interface ITimeoutState : IMessage
    {
    }
}