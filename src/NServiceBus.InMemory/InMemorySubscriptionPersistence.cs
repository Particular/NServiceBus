namespace NServiceBus.Persistence.InMemory;

using Features;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionStorage;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

class InMemorySubscriptionPersistence : Feature
{
    public InMemorySubscriptionPersistence() => DependsOn("NServiceBus.Features.MessageDrivenSubscriptions");

    protected override void Setup(FeatureConfigurationContext context)
    {
        var configuredStorage = context.Settings.GetOrDefault<InMemoryStorage>(InMemoryStorageRuntime.StorageKey);
        InMemoryStorageRuntime.Configure(context.Services, configuredStorage);
        context.Services.AddSingleton<InMemorySubscriptionStorage>(sp => new InMemorySubscriptionStorage(sp.GetRequiredService<InMemoryStorage>()));
        context.Services.AddSingleton<ISubscriptionStorage>(sp => sp.GetRequiredService<InMemorySubscriptionStorage>());
    }
}