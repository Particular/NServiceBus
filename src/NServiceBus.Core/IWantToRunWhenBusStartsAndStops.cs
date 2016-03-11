namespace NServiceBus
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Implementers will be invoked when the endpoint starts up.
    /// Dependency injection is provided for these types.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IWantToRunWhenBusStartsAndStops
    {
        /// <summary>
        /// Method called at startup.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task Start(IMessageSession session);

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task Stop(IMessageSession session);
    }
}