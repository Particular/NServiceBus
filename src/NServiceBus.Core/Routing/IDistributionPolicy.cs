namespace NServiceBus
{
    using Routing;

    /// <summary>
    /// Provides access to <see cref="DistributionStrategy"/>.
    /// </summary>
    public interface IDistributionPolicy
    {
        /// <summary>
        /// Returns a <see cref="DistributionStrategy"/> for a given logical endpoint.
        /// </summary>
        DistributionStrategy GetDistributionStrategy(string endpointName, DistributionStrategyScope scope);
    }
}