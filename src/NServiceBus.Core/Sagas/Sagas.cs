namespace NServiceBus.Features;

using System;
using System.Linq;
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
        Defaults(s => s.SetDefault(new SagaMetadataCollection()));

        Enable<SynchronizedStorage>();

        DependsOn<SynchronizedStorage>();

        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Sagas are only relevant for endpoints receiving messages.");
        Prerequisite(context =>
        {
            var sagaCollection = context.Settings.Get<SagaMetadataCollection>();
            sagaCollection.Initialize(context.Settings.GetAvailableTypes());
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

        var conventions = context.Settings.Get<Conventions>();
        var timeoutMessages = sagaMetaModel.SelectMany(m => m.AssociatedMessages).Where(m => m.IsTimeout).Select(m => m.MessageType)
            .ToHashSet();
        if (timeoutMessages.Count > 0)
        {
            conventions.AddSystemMessagesConventions(t => timeoutMessages.Contains(t));
        }

        // Register saga not found handlers
        foreach (var t in context.Settings.GetAvailableTypes())
        {
            if (IsSagaNotFoundHandler(t))
            {
                context.Services.AddTransient(typeof(IHandleSagaNotFound), t);
            }
        }

        // Register the Saga related behaviors for incoming messages
        context.Pipeline.Register("InvokeSaga", b => new SagaPersistenceBehavior(b.GetRequiredService<ISagaPersister>(), sagaIdGenerator, sagaMetaModel), "Invokes the saga logic");
        context.Pipeline.Register("InvokeSagaNotFound", new InvokeSagaNotFoundBehavior(), "Invokes saga not found logic");
        context.Pipeline.Register("AttachSagaDetailsToOutGoingMessage", new AttachSagaDetailsToOutGoingMessageBehavior(), "Makes sure that outgoing messages have saga info attached to them");
    }

    static bool IsSagaNotFoundHandler(Type t) => IsCompatible(t, typeof(IHandleSagaNotFound));

    static bool IsCompatible(Type t, Type source) => source.IsAssignableFrom(t) && t != source && !t.IsAbstract && !t.IsInterface && !t.IsGenericType;
}