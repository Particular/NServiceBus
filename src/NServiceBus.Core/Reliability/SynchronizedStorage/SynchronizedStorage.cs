namespace NServiceBus.Features;

using Microsoft.Extensions.DependencyInjection;
using Persistence;

/// <summary>
/// Configures the synchronized storage.
/// </summary>
public class SynchronizedStorage : Feature, IFeatureFactory
{
    internal SynchronizedStorage()
    {
    }

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context) => context.Services.AddScoped<ISynchronizedStorageSession>(provider => provider.GetService<ICompletableSynchronizedStorageSession>());

    static Feature IFeatureFactory.Create() => new SynchronizedStorage();
}