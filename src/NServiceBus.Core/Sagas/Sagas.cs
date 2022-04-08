namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Sagas;

    /// <summary>
    /// Used to configure saga.
    /// </summary>
    public class Sagas : Feature
    {
        internal Sagas()
        {
            EnableByDefault();

            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();

                s.Set(new SagaMetadataCollection());

                conventions = s.Get<Conventions>();

                var sagas = s.GetAvailableTypes().Where(IsSagaType).ToList();
                if (sagas.Count > 0)
                {
                    conventions.AddSystemMessagesConventions(t => IsTypeATimeoutHandledByAnySaga(t, sagas));
                }
            });

            DependsOn<SynchronizedStorage>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Sagas are only relevant for endpoints receiving messages.");
            Prerequisite(config => config.Settings.GetAvailableTypes().Any(IsSagaType), "No sagas were found in the scanned types");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.Sagas>(context.Settings))
            {
                throw new Exception("The selected persistence doesn't have support for saga storage. Select another persistence or disable the sagas feature using endpointConfiguration.DisableFeature<Sagas>()");
            }

            var sagaIdGenerator = context.Settings.GetOrDefault<ISagaIdGenerator>() ?? new DefaultSagaIdGenerator();

            var sagaMetaModel = context.Settings.Get<SagaMetadataCollection>();
            sagaMetaModel.Initialize(context.Settings.GetAvailableTypes(), conventions);

            var verifyIfEntitiesAreShared = !context.Settings.GetOrDefault<bool>(SagaSettings.DisableVerifyingIfEntitiesAreShared);

            if (verifyIfEntitiesAreShared)
            {
                sagaMetaModel.VerifyIfEntitiesAreShared();
            }

            RegisterCustomFindersInContainer(context.Container, sagaMetaModel);

            foreach (var t in context.Settings.GetAvailableTypes())
            {
                if (IsSagaNotFoundHandler(t))
                {
                    context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
                }
            }

            // Register the Saga related behaviors for incoming messages
            context.Pipeline.Register("InvokeSaga", b => new SagaPersistenceBehavior(b.GetRequiredService<ISagaPersister>(), sagaIdGenerator, sagaMetaModel), "Invokes the saga logic");
            context.Pipeline.Register("InvokeSagaNotFound", new InvokeSagaNotFoundBehavior(), "Invokes saga not found logic");
            context.Pipeline.Register("AttachSagaDetailsToOutGoingMessage", new AttachSagaDetailsToOutGoingMessageBehavior(), "Makes sure that outgoing messages have saga info attached to them");
        }

        static void RegisterCustomFindersInContainer(IServiceCollection container, IEnumerable<SagaMetadata> sagaMetaModel)
        {
            foreach (var finder in sagaMetaModel.SelectMany(m => m.Finders))
            {
                container.ConfigureComponent(finder.Type, DependencyLifecycle.InstancePerCall);

                if (finder.Properties.TryGetValue("custom-finder-clr-type", out var customFinderType))
                {
                    container.ConfigureComponent((Type)customFinderType, DependencyLifecycle.InstancePerCall);
                }
            }
        }

        static bool IsSagaType(Type t)
        {
            return IsCompatible(t, typeof(Saga));
        }

        static bool IsSagaNotFoundHandler(Type t)
        {
            return IsCompatible(t, typeof(IHandleSagaNotFound));
        }

        static bool IsCompatible(Type t, Type source)
        {
            return source.IsAssignableFrom(t) && t != source && !t.IsAbstract && !t.IsInterface && !t.IsGenericType;
        }

        static bool IsTypeATimeoutHandledByAnySaga(Type type, IEnumerable<Type> sagas)
        {
            var timeoutHandler = typeof(IHandleTimeouts<>).MakeGenericType(type);
            var messageHandler = typeof(IHandleMessages<>).MakeGenericType(type);

            return sagas.Any(t => timeoutHandler.IsAssignableFrom(t) && !messageHandler.IsAssignableFrom(t));
        }

        Conventions conventions;
    }
}