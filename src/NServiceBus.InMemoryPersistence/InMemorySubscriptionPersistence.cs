namespace NServiceBus.Persistence.InMemory;

using Features;
using Microsoft.Extensions.DependencyInjection;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

class InMemorySubscriptionPersistence : Feature
{
    public InMemorySubscriptionPersistence() => DependsOn("NServiceBus.Features.MessageDrivenSubscriptions");

    protected override void Setup(FeatureConfigurationContext context)
        => context.Services.AddSingleton<ISubscriptionStorage, SubscriptionStorage.InMemorySubscriptionStorage>();
}