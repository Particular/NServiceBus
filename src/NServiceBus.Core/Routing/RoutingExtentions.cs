namespace NServiceBus.Routing
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// This class provides implementers of <see cref="IRouterDistributor"/> with an extension mechanism for custom settings via extention methods.
    /// </summary>
    /// <typeparam name="T">The client distributor definition eg <see cref="FileBasedDynamicRouting"/>.</typeparam>
    public class RoutingExtentions<T> : ExposeSettings where T : DynamicRoutingDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RoutingExtentions(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Overrides the default logical address translator to the parameter that is passed to <see cref="IRouterDistributor.TryGetRouteAddress"/>.
        /// </summary>
        /// <remarks>
        /// The default translator uses the <see cref="Address.ToString()"/>.
        /// </remarks>
        /// <param name="translateToLogicalAddress">The callback to do the translation.</param>
        public RoutingExtentions<T> WithTranslator(Func<Address, string> translateToLogicalAddress)
        {
            Settings.Set("Routing.Translator", translateToLogicalAddress);
            return this;
        }
    }
}