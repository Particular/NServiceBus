namespace NServiceBus.AcceptanceTesting;

using Features;
using Microsoft.Extensions.DependencyInjection;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

class AcceptanceTestingSubscriptionPersistence : Feature, IFeatureFactory
{
    public AcceptanceTestingSubscriptionPersistence()
    {
        DependsOn("NServiceBus.Features.MessageDrivenSubscriptions");
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Services.AddSingleton<ISubscriptionStorage, AcceptanceTestingSubscriptionStorage>();
    }

    static Feature IFeatureFactory.Create() => new AcceptanceTestingSubscriptionPersistence();
}