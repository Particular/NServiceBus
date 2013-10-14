namespace NServiceBus.Saga
{
    /// <summary>
    /// Tells the infrastructure that the user wants to handle a timeout of T
    /// </summary>
    public interface IHandleTimeouts<T>
    {
        /// <summary>
        /// Called when the timeout has expired
        /// </summary>
        void Timeout(T state);
    }
}