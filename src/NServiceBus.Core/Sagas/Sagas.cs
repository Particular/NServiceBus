namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NServiceBus.Saga;
    using NServiceBus.Sagas;
    using NServiceBus.Sagas.Finders;

    /// <summary>
    ///     Used to configure saga.
    /// </summary>
    public class Sagas : Feature
    {
        internal Sagas()
        {
            sagaConfigurationCache = new SagaConfigurationCache();
            sagaMessageFindingConfiguration = new ConfigureHowToFindSagaWithMessageDispatcher(sagaConfigurationCache);
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

            Prerequisite(config => config.Settings.GetAvailableTypes().Any(IsSagaType), "No sagas was found in scabbed types");
        }

        /// <summary>
        ///     See <see cref="Feature.Setup" />
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            // Register the Saga related behavior for incoming messages
            context.Pipeline.Register<SagaPersistenceBehavior.SagaPersistenceRegistration>();

            context.Container.RegisterSingleton(sagaConfigurationCache);

            foreach (var t in context.Settings.GetAvailableTypes())
            {
                if (IsSagaType(t))
                {
                    context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
                    ConfigureSaga(t, conventions);
                    continue;
                }

                if (IsFinderType(t))
                {
                    context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
                    ConfigureFinder(t, conventions);
                    continue;
                }

                if (IsSagaNotFoundHandler(t))
                {
                    context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall);
                }
            }

            CreateAdditionalFindersAsNecessary(context);
        }


        void CreateAdditionalFindersAsNecessary(FeatureConfigurationContext context)
        {
            foreach (var sagaEntityPair in sagaConfigurationCache.SagaEntityToMessageToPropertyLookup)
            {
                foreach (var messageType in sagaEntityPair.Value.Keys)
                {
                    var sagaToMessageMap = sagaEntityPair.Value[messageType];
                    CreatePropertyFinder(context, sagaEntityPair.Key, messageType, sagaToMessageMap);
                }
            }

            foreach (var sagaEntityType in sagaConfigurationCache.SagaTypeToSagaEntityTypeLookup.Values)
            {
                var sagaHeaderIdFinder = typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType);
                context.Container.ConfigureComponent(sagaHeaderIdFinder, DependencyLifecycle.InstancePerCall);
                ConfigureFinder(sagaHeaderIdFinder, conventions);
            }
        }

        void CreatePropertyFinder(FeatureConfigurationContext context, Type sagaEntityType, Type messageType, SagaToMessageMap sagaToMessageMap)
        {
            var finderType = typeof(PropertySagaFinder<,>).MakeGenericType(sagaEntityType, messageType);

            context.Container.ConfigureComponent(finderType, DependencyLifecycle.InstancePerCall)
                .ConfigureProperty("SagaToMessageMap", sagaToMessageMap);

            ConfigureFinder(finderType, conventions);
        }

        // Made internal for testing purposes
        internal void ConfigureSaga(Type t, Conventions conventions)
        {
            foreach (var messageType in GetMessageTypesHandledBySaga(t, conventions))
            {
                MapMessageTypeToSagaType(messageType, t);
            }

            foreach (var messageType in GetMessageTypesThatRequireStartingTheSaga(t, conventions))
            {
                MessageTypeRequiresStartingSaga(messageType, t);
            }

            var prop = t.GetProperty("Data");
            MapSagaTypeToSagaEntityType(t, prop.PropertyType);

            var saga = (Saga) FormatterServices.GetUninitializedObject(t);
            saga.ConfigureHowToFindSaga(sagaMessageFindingConfiguration);
        }

        // Made internal for testing purposes
        internal void ConfigureFinder(Type t, Conventions conventions)
        {
            foreach (var interfaceType in t.GetInterfaces())
            {
                var args = interfaceType.GetGenericArguments();
                if (args.Length != 2)
                {
                    continue;
                }

                Type sagaEntityType = null;
                Type messageType = null;
                foreach (var type in args)
                {
                    if (typeof(IContainSagaData).IsAssignableFrom(type))
                    {
                        sagaEntityType = type;
                    }

                    if (conventions.IsMessageType(type) || type == typeof(object))
                    {
                        messageType = type;
                    }
                }

                if (sagaEntityType == null || messageType == null)
                {
                    continue;
                }

                var finderType = typeof(IFindSagas<>.Using<>).MakeGenericType(sagaEntityType, messageType);
                if (!finderType.IsAssignableFrom(t))
                {
                    continue;
                }

                finderTypeToSagaEntityTypeLookup[t] = sagaEntityType;

                var method = t.GetMethod("FindBy", new[]
                {
                    messageType
                });

                Dictionary<Type, MethodInfo> methods;
                sagaConfigurationCache.FinderTypeToMessageToMethodInfoLookup.TryGetValue(t, out methods);

                if (methods == null)
                {
                    methods = new Dictionary<Type, MethodInfo>();
                    sagaConfigurationCache.FinderTypeToMessageToMethodInfoLookup[t] = methods;
                }

                methods[messageType] = method;
            }
        }
        
        void MapMessageTypeToSagaType(Type messageType, Type sagaType)
        {
            List<Type> sagas;
            sagaConfigurationCache.MessageTypeToSagaTypesLookup.TryGetValue(messageType, out sagas);

            if (sagas == null)
            {
                sagas = new List<Type>(1);
                sagaConfigurationCache.MessageTypeToSagaTypesLookup[messageType] = sagas;
            }

            if (!sagas.Contains(sagaType))
            {
                sagas.Add(sagaType);
            }
        }

        void MapSagaTypeToSagaEntityType(Type sagaType, Type sagaEntityType)
        {
            sagaConfigurationCache.SagaTypeToSagaEntityTypeLookup[sagaType] = sagaEntityType;
            sagaEntityTypeToSagaTypeLookup[sagaEntityType] = sagaType;
        }

        void MessageTypeRequiresStartingSaga(Type messageType, Type sagaType)
        {
            List<Type> messages;
            sagaConfigurationCache.SagaTypeToMessageTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messages);

            if (messages == null)
            {
                messages = new List<Type>(1);
                sagaConfigurationCache.SagaTypeToMessageTypesRequiringSagaStartLookup[sagaType] = messages;
            }

            if (!messages.Contains(messageType))
            {
                messages.Add(messageType);
            }
        }

        static bool IsSagaType(Type t)
        {
            return IsCompatible(t, typeof(Saga));
        }

        static bool IsFinderType(Type t)
        {
            return IsCompatible(t, typeof(IFinder));
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

        internal static IEnumerable<Type> GetMessageTypesHandledBySaga(Type sagaType, Conventions conventions)
        {
            return GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleMessages<>), conventions);
        }

        internal static IEnumerable<Type> GetMessageTypesThatRequireStartingTheSaga(Type sagaType, Conventions conventions)
        {
            return GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IAmStartedByMessages<>), conventions);
        }

        static IEnumerable<Type> GetMessagesCorrespondingToFilterOnSaga(Type sagaType, Type filter, Conventions conventions)
        {
            foreach (var interfaceType in sagaType.GetInterfaces())
            {
                foreach (var argument in interfaceType.GetGenericArguments())
                {
                    var genericType = filter.MakeGenericType(argument);
                    var isOfFilterType = genericType == interfaceType;
                    if (!isOfFilterType)
                    {
                        continue;
                    }
                    if (conventions.IsMessageType(argument))
                    {
                        yield return argument;
                        continue;
                    }
                    var message = string.Format("The saga '{0}' implements '{1}' but the message type '{2}' is not classified as a message. You should either use 'Unobtrusive Mode Messages' or the message should implement either 'IMessage', 'IEvent' or 'ICommand'.", sagaType.FullName, genericType.Name, argument.FullName);
                    throw new Exception(message);
                }
            }
        }

        Conventions conventions;
        Dictionary<Type, Type> finderTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();
        internal SagaConfigurationCache sagaConfigurationCache; // Made internal for testing purposes
        Dictionary<Type, Type> sagaEntityTypeToSagaTypeLookup = new Dictionary<Type, Type>();
        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
    }
}