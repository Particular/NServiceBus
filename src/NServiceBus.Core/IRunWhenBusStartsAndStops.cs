namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Implementers will be invoked when the endpoint starts up.
    /// Dependency injection is provided for these types.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IRunWhenBusStartsAndStops
    {
        /// <summary>
        /// Method called at startup.
        /// </summary>
        void Start(IRunContext context);

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        void Stop(IRunContext context);
    }
}