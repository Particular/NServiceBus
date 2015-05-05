namespace NServiceBus.Saga
{
    using JetBrains.Annotations;

    /// <summary>
    /// Tells the infrastructure that the user wants to handle a timeout of T
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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

    public interface ITimeoutContext
    {
        void SendLocal(object message);
    }

    internal class TimeoutContext : ITimeoutContext
    {
        readonly IBus bus;

        public TimeoutContext(IBus bus)
        {
            this.bus = bus;
        }

        public void SendLocal(object message)
        {
            bus.SendLocal(message);
        }
    }
#pragma warning restore 1591
}