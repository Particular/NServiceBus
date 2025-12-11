namespace NServiceBus.Features;

using System;
using System.Linq;
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
        Defaults(s => s.SetDefault(new SagaMetadataCollection()));

        Enable<SynchronizedStorage>();

        DependsOn<SynchronizedStorage>();

        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Sagas are only relevant for endpoints receiving messages.");
        Prerequisite(context =>
        {
            var sagaCollection = context.Settings.Get<SagaMetadataCollection>();
            var sagaMetadata = SagaMetadata.CreateMany(context.Settings.GetAvailableTypes());
            sagaCollection.AddRange(sagaMetadata);
            return sagaCollection.HasMetadata;
        }, "No sagas were found. Either enable assembly scanning or manually register sagas using AddSaga<TSaga>().");
    }

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
    {
        if (!context.HasSupportForStorage<StorageType.Sagas>())
        {
            throw new Exception("The selected persistence doesn't have support for saga storage. Select another persistence or disable the sagas feature using endpointConfiguration.DisableFeature<Sagas>()");
        }

        var sagaIdGenerator = context.Settings.GetOrDefault<ISagaIdGenerator>() ?? new DefaultSagaIdGenerator();

        var sagaMetaModel = context.Settings.Get<SagaMetadataCollection>();
        sagaMetaModel.PreventChanges();

        if (context.GetStorageOptions<StorageType.SagasOptions>() is { SupportsFinders: false })
        {
            var customerFinders = (from s in sagaMetaModel
                                   from finder in s.Finders
                                   where finder.SagaFinder.IsCustomFinder
                                   group s by s.SagaType).ToArray();

            if (customerFinders.Length != 0)
            {
                throw new Exception(
                    "The selected persistence doesn't support custom sagas finders. The following sagas use custom finders: " +
                    string.Join(", ", customerFinders.Select(g => g.Key.FullName)) + ".");
            }
        }

        var verifyIfEntitiesAreShared = !context.Settings.GetOrDefault<bool>(SagaSettings.DisableVerifyingIfEntitiesAreShared);

        if (verifyIfEntitiesAreShared)
        {
            sagaMetaModel.VerifyIfEntitiesAreShared();
        }

        var messageMetadataRegistry = context.Settings.Get<MessageMetadataRegistry>();
        // Register all messages associated with sagas and assume they are message types, therefore we are not using the conventions
        messageMetadataRegistry.RegisterMessageTypes(sagaMetaModel.SelectMany(s => s.AssociatedMessages.Select(m => m.MessageType)));

        // Register the Saga related behaviors for incoming messages
        context.Pipeline.Register("InvokeSaga", b => new SagaPersistenceBehavior(b.GetRequiredService<ISagaPersister>(), sagaIdGenerator, sagaMetaModel, b), "Invokes the saga logic");
        context.Pipeline.Register("AttachSagaDetailsToOutGoingMessage", new AttachSagaDetailsToOutGoingMessageBehavior(), "Makes sure that outgoing messages have saga info attached to them");
    }
}