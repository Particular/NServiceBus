namespace NServiceBus
{
    using Routing;

    interface IDistributionPolicy
    {
        DistributionStrategy GetDistributionStrategy(string endpointName, DistributionStrategyScope scope);
    }
}