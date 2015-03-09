namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Indicate that the implementing class will specify configuration.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IConfigureThisEndpoint
    {
        /// <summary>
        /// Allows to override default settings.
        /// </summary>
        /// <param name="configuration">Endpoint configuration builder.</param>
        void Customize(BusConfiguration configuration);
    }
}
