namespace NServiceBus.Saga
{
    /// <summary>
    /// Interface used by the saga infrastructure for notifying sagas about a timeout.
    /// </summary>
    [ObsoleteEx(Message = "2.6 style timeouts has been replaced. Please implement IHandleTimeouts<T> instead.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]    
    public interface ITimeoutable
    {
        /// <summary>
        /// Indicates to the saga that a timeout has occurred, 
        /// passing in the state object previously received from the saga.
        /// </summary>
        void Timeout(object state);
    }
}
