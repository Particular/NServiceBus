namespace NServiceBus.Features;

using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Sagas;
using Unicast.Messages;

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
        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Sagas are only relevant for endpoints receiving messages.");
        Prerequisite(s => s.Settings.Get<SagaMetadataCollection>().HasMetadata, "No sagas were found. Either enable assembly scanning or manually register sagas using AddSaga<TSaga>().");
    }

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
    {
        var sagaIdGenerator = context.Settings.GetOrDefault<ISagaIdGenerator>() ?? new DefaultSagaIdGenerator();
        var sagaMetaModel = context.Settings.Get<SagaMetadataCollection>();
        var supportsTimeoutManagement = context.SagaStorageSupportsTimeoutManagement;

        // Register the Saga related behaviors for incoming messages
        context.Pipeline.Register("InvokeSaga", b =>
        {
            var persister = b.GetRequiredService<ISagaPersister>();
            var timeoutPersister = supportsTimeoutManagement ? b.GetRequiredService<ITimeoutPersister>() : null;
            // Timeout changes are reported only when reporting is enabled and the saga persistence supports timeout management
            var platformNotifier = supportsTimeoutManagement ? b.GetService<SagaTimeoutPlatformNotifier>() : null;

            return new SagaPersistenceBehavior(persister, sagaIdGenerator, sagaMetaModel, b.GetRequiredService<TimeoutSerializer>(), b.GetRequiredService<MessageMetadataRegistry>(), platformNotifier!, timeoutPersister!, b);
        }, "Invokes the saga logic");
        context.Pipeline.Register("AttachSagaDetailsToOutGoingMessage", new AttachSagaDetailsToOutGoingMessageBehavior(), "Makes sure that outgoing messages have saga info attached to them");
    }
}