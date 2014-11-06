namespace NServiceBus.Unicast.Routing
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// This class provides implementers of <see cref="IRouterDistributor"/> with an extension mechanism for custom settings via extention methods.
    /// </summary>
    /// <typeparam name="T">The client distributor definition eg <see cref="FileBasedRoutingDistributor"/>.</typeparam>
    public class RoutingExtentions<T> : ExposeSettings where T : RoutingDistributorDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RoutingExtentions(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Overrides the default translator of <see cref="Address"/> to the parameter that is passed to <see cref="IRouterDistributor.TryGetRouteAddress"/>.
        /// </summary>
        /// <remarks>
        /// The default translator uses the <see cref="Address.Queue"/>.
        /// </remarks>
        /// <param name="translateAddressToQueueName">The callback to do the translation.</param>
        public RoutingExtentions<T> WithTranslator(Func<Address, string> translateAddressToQueueName)
        {
            Settings.Set("Routing.Translator", translateAddressToQueueName);
            return this;
        }
    }
}