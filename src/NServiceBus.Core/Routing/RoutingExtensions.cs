namespace NServiceBus.Routing
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// This class provides implementers of <see cref="IProvideDynamicRouting"/> with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The client distributor definition eg <see cref="FileBasedRoundRobinDistribution"/>.</typeparam>
    public class RoutingExtensions<T> : ExposeSettings where T : DynamicRoutingDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RoutingExtensions(SettingsHolder settings)
            : base(settings)
        {
        }
    }
}