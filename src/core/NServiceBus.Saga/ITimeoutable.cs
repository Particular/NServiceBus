namespace NServiceBus.Saga
{
    /// <summary>
    /// Interface used by the saga infrastructure for notifying sagas about a timeout.
    /// </summary>
    public interface ITimeoutable
    {
        /// <summary>
        /// Indicates to the saga that a timeout has occurred, 
        /// passing in the state object previously received from the saga.
        /// </summary>
        /// <param name="state"></param>
        void Timeout(object state);
    }
}
