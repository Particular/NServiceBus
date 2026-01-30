namespace NServiceBus.Features;

using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Sagas;

/// <summary>
/// Used to configure saga.
/// </summary>
public sealed class Sagas : Feature
{
    /// <summary>
    /// Creates a new instance of the saga feature.
    /// </summary>
    public Sagas()
    {
        Enable<SynchronizedStorage>();
        DependsOn<SynchronizedStorage>();
        Prerequisite(s => s.Settings.Get<SagaMetadataCollection>().HasMetadata, "No sagas were found. Either enable assembly scanning or manually register sagas using AddSaga<TSaga>().");
    }

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
    {
        var sagaIdGenerator = context.Settings.GetOrDefault<ISagaIdGenerator>() ?? new DefaultSagaIdGenerator();
        var sagaMetaModel = context.Settings.Get<SagaMetadataCollection>();

        // Register the Saga related behaviors for incoming messages
        context.Pipeline.Register("InvokeSaga", b => new SagaPersistenceBehavior(b.GetRequiredService<ISagaPersister>(), sagaIdGenerator, sagaMetaModel, b), "Invokes the saga logic");
        context.Pipeline.Register("AttachSagaDetailsToOutGoingMessage", new AttachSagaDetailsToOutGoingMessageBehavior(), "Makes sure that outgoing messages have saga info attached to them");
    }
}