namespace NServiceBus.Saga
{
    /// <summary>
    /// Tells the infrastructure that the user wants to handle a timeout of T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHandleTimeouts<T>
    {
        /// <summary>
        /// Called when the timout has expired
        /// </summary>
        /// <param name="state"></param>
        void Timeout(T state);
    }
}