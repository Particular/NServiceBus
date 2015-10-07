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
        Task StartAsync(IBusInterface bus);

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        Task StopAsync();
    }
}
