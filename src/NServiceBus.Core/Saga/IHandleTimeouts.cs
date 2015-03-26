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
    public interface IConsumeTimeout<T>
    {
        void Timeout(T state, IConsumeTimeoutContext context);
    }

    public interface IConsumeTimeoutContext { }

    internal class ConsumeTimeoutContext : IConsumeTimeoutContext
    {
    }
#pragma warning restore 1591
}