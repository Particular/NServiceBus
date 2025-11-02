#nullable enable

namespace NServiceBus;

using Features;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

sealed class LearningSynchronizedStorage : Feature
{
    public LearningSynchronizedStorage() => DependsOn<SynchronizedStorage>();

    protected override void Setup(FeatureConfigurationContext context) => context.Services.AddScoped<ICompletableSynchronizedStorageSession, LearningSynchronizedStorageSession>();
}