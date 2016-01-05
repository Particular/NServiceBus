﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder;
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
                conventions = s.Get<Conventions>();

                var sagas = s.GetAvailableTypes().Where(IsSagaType).ToList();
                if (sagas.Count > 0)
                {
                    conventions.AddSystemMessagesConventions(t => IsTypeATimeoutHandledByAnySaga(t, sagas));
                }
            });

            Defaults(s => s.Set<SagaMetadataCollection>(new SagaMetadataCollection()));

            Prerequisite(config => config.Settings.GetAvailableTypes().Any(IsSagaType), "No sagas was found in scanned types");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            // Register the Saga related behaviors for incoming messages
            context.Pipeline.Register<PersistSagaBehavior.Registration>();
            context.Pipeline.Register<LoadSagaBehavior.Registration>();
            context.Pipeline.Register("AttachSagaDetailsToOutGoingMessage", typeof(AttachSagaDetailsToOutGoingMessageBehavior), "Makes sure that outgoing messages have saga info attached to them");

            var sagaMetaModel = context.Settings.Get<SagaMetadataCollection>();
            sagaMetaModel.Initialize(context.Settings.GetAvailableTypes(), conventions);

            RegisterCustomFindersInContainer(context.Container, sagaMetaModel);

            context.Container.RegisterSingleton(sagaMetaModel);

            foreach (var t in context.Settings.GetAvailableTypes())
            {
                if (IsSagaNotFoundHandler(t))
                {
                    context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
                }
            }
        }

        static void RegisterCustomFindersInContainer(IConfigureComponents container, IEnumerable<SagaMetadata> sagaMetaModel)
        {
            foreach (var finder in sagaMetaModel.SelectMany(m => m.Finders))
            {
                container.ConfigureComponent(finder.Type, DependencyLifecycle.InstancePerCall);

                object customFinderType;

                if (finder.Properties.TryGetValue("custom-finder-clr-type", out customFinderType))
                {
                    container.ConfigureComponent((Type) customFinderType, DependencyLifecycle.InstancePerCall);
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