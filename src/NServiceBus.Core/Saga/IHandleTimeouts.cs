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
    // Daniel :This is not thought through and serves as a discussion point. Should we have timeout context. If yes should it contain the state and contextual information including contextual bus.
    public interface IHandleTimeout<T>
    {
        void Timeout(T state, TimeoutContext context);
    }
    public class TimeoutContext
    {
    }
#pragma warning restore 1591
}