namespace NServiceBus.Features;

using Microsoft.Extensions.DependencyInjection;
using Persistence;

/// <summary>
/// Configures the synchronized storage.
/// </summary>
public sealed class SynchronizedStorage : Feature
{
    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context) => context.Services.AddScoped<ISynchronizedStorageSession>(provider => provider.GetService<ICompletableSynchronizedStorageSession>());
}