namespace NServiceBus
{
    using Routing;

    /// <summary>
    /// Defines a policy to find the <see cref="DistributionStrategy"/> to apply sender-side distribution to outgoing messages.
    /// </summary>
    public interface IDistributionPolicy
    {
        /// <summary>
        /// Returns the <see cref="DistributionStrategy"/> for a given logical endpoint and the defined strategy scope.
        /// </summary>
        /// <param name="endpointName">The logical endpoints receiving the message.</param>
        /// <param name="scope">The scope of the outgoing message.</param>
        /// <returns>A strategy to resolve the instances which should receive the message.</returns>
        DistributionStrategy GetDistributionStrategy(string endpointName, DistributionStrategyScope scope);
    }
}