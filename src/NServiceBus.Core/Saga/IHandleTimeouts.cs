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
#pragma warning disable 1591
    public interface IProcessTimeouts<T>
    {
        void Timeout(T state, ITimeoutContext context);
    }

    public interface ITimeoutContext { }

    internal class TimeoutContext : ITimeoutContext
    {
    }
#pragma warning restore 1591
}