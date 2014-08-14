﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NServiceBus.Sagas;
    using NServiceBus.Sagas.Finders;
    using Saga;

    /// <summary>
    /// Used to configure saga.
    /// </summary>
    public class Sagas : Feature
    {
        internal Sagas()
        {
            EnableByDefault();

            Prerequisite(config => config.Settings.GetAvailableTypes().Any(IsSagaType), "No sagas was found in scabbed types");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            conventions = context.Settings.Get<Conventions>();

            var sagas = context.Settings.GetAvailableTypes().Where(IsSagaType).ToList();
            conventions.AddSystemMessagesConventions(t => IsTypeATimeoutHandledByAnySaga(t, sagas));
            
            // Register the Saga related behavior for incoming messages
            context.Pipeline.Register<SagaPersistenceBehavior.SagaPersistenceRegistration>();

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

        internal static void ConfigureHowToFindSagaWithMessage(Type sagaType, Type messageType, SagaToMessageMap sagaToMessageMap)
        {
            Dictionary<Type, SagaToMessageMap> messageToProperties;
            SagaEntityToMessageToPropertyLookup.TryGetValue(sagaType, out messageToProperties);

            if (messageToProperties == null)
            {
                messageToProperties = new Dictionary<Type, SagaToMessageMap>();
                SagaEntityToMessageToPropertyLookup[sagaType] = messageToProperties;
            }

            messageToProperties[messageType] = sagaToMessageMap;
        }

        internal static readonly Dictionary<Type, Dictionary<Type, SagaToMessageMap>> SagaEntityToMessageToPropertyLookup = new Dictionary<Type, Dictionary<Type, SagaToMessageMap>>();

        void CreateAdditionalFindersAsNecessary(FeatureConfigurationContext context)
        {
            foreach (var sagaEntityPair in SagaEntityToMessageToPropertyLookup)
            {
                foreach (var messageType in sagaEntityPair.Value.Keys)
                {
                    var sagaToMessageMap = sagaEntityPair.Value[messageType];
                    CreatePropertyFinder(context, sagaEntityPair.Key, messageType, sagaToMessageMap);
                }
            }

            foreach (var sagaEntityType in SagaTypeToSagaEntityTypeLookup.Values)
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

        /// <summary>
        /// True if the given message are configure to start the saga
        /// </summary>
        internal static bool IsAStartSagaMessage(Type sagaType, Type messageType)
        {
            List<Type> messageTypes;
            SagaTypeToMessageTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messageTypes);

            if (messageTypes == null)
                return false;

            if (messageTypes.Contains(messageType))
                return true;

            return messageTypes.Any(msgTypeHandleBySaga => msgTypeHandleBySaga.IsAssignableFrom(messageType));
        }

        /// <summary>
        /// Returns the saga type configured for the given entity type.
        /// </summary>
        internal static Type GetSagaTypeForSagaEntityType(Type sagaEntityType)
        {
            Type result;
            SagaEntityTypeToSagaTypeLookup.TryGetValue(sagaEntityType, out result);

            return result;
        }

        /// <summary>
        /// Returns the entity type configured for the given saga type.
        /// </summary>
        internal static Type GetSagaEntityTypeForSagaType(Type sagaType)
        {
            Type result;
            SagaTypeToSagaEntityTypeLookup.TryGetValue(sagaType, out result);

            return result;
        }

        /// <summary>
        /// Gets a reference to the generic "FindBy" method of the given finder
        /// for the given message type using a hashtable lookup rather than reflection.
        /// </summary>
        internal static MethodInfo GetFindByMethodForFinder(IFinder finder, object message)
        {
            MethodInfo result = null;

            Dictionary<Type, MethodInfo> methods;
            FinderTypeToMessageToMethodInfoLookup.TryGetValue(finder.GetType(), out methods);

            if (methods != null)
            {
                methods.TryGetValue(message.GetType(), out result);

                if (result == null)
                {
                    foreach (var messageTypePair in methods)
                    {
                        if (messageTypePair.Key.IsInstanceOfType(message))
                        {
                            result = messageTypePair.Value;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a list of finder object capable of using the given message.
        /// </summary>
        internal static IEnumerable<Type> GetFindersForMessageAndEntity(Type messageType, Type entityType)
        {
            var findersWithExactMatch = new List<Type>();
            var findersMatchingBaseTypes = new List<Type>();

            foreach (var finderPair in FinderTypeToMessageToMethodInfoLookup)
            {
                var messageToMethodInfo = finderPair.Value;

                MethodInfo methodInfo;

                if (messageToMethodInfo.TryGetValue(messageType, out methodInfo) && methodInfo.ReturnType == entityType)
                {
                    findersWithExactMatch.Add(finderPair.Key);
                }
                else
                {
                    foreach (var otherMessagePair in messageToMethodInfo)
                    {
                        if (otherMessagePair.Key.IsAssignableFrom(messageType) && otherMessagePair.Value.ReturnType == entityType)
                        {
                            findersMatchingBaseTypes.Add(finderPair.Key);
                        }
                    }
                }
            }

            return findersWithExactMatch.Concat(findersMatchingBaseTypes);
        }

        /// <summary>
        /// Returns the list of saga types configured.
        /// </summary>
        internal static IEnumerable<Type> GetSagaDataTypes()
        {
            return SagaTypeToSagaEntityTypeLookup.Values;
        }

        internal static bool IsSagaType(Type t)
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

        internal static void ConfigureSaga(Type t, Conventions conventions)
        {
            foreach (var messageType in GetMessageTypesHandledBySaga(t, conventions))
                MapMessageTypeToSagaType(messageType, t);

            foreach (var messageType in GetMessageTypesThatRequireStartingTheSaga(t, conventions))
                MessageTypeRequiresStartingSaga(messageType, t);

            var prop = t.GetProperty("Data");
            MapSagaTypeToSagaEntityType(t, prop.PropertyType);

            var saga = (Saga)FormatterServices.GetUninitializedObject(t);
            saga.ConfigureHowToFindSaga(SagaMessageFindingConfiguration);
        }

        static bool IsTypeATimeoutHandledByAnySaga(Type type, IEnumerable<Type> sagas)
        {
            var timeoutHandler = typeof(IHandleTimeouts<>).MakeGenericType(type);
            var messageHandler = typeof(IHandleMessages<>).MakeGenericType(type);

            return sagas.Any(t => timeoutHandler.IsAssignableFrom(t) && !messageHandler.IsAssignableFrom(t));
        }

        internal static void ConfigureFinder(Type t, Conventions conventions)
        {
            foreach (var interfaceType in t.GetInterfaces())
            {
                var args = interfaceType.GetGenericArguments();
                if (args.Length != 2)
                    continue;

                Type sagaEntityType = null;
                Type messageType = null;
                foreach (var type in args)
                {
                    if (typeof(IContainSagaData).IsAssignableFrom(type))
                        sagaEntityType = type;

                    if (conventions.IsMessageType(type) || type == typeof(object))
                        messageType = type;
                }

                if (sagaEntityType == null || messageType == null)
                    continue;

                var finderType = typeof(IFindSagas<>.Using<>).MakeGenericType(sagaEntityType, messageType);
                if (!finderType.IsAssignableFrom(t))
                    continue;

                FinderTypeToSagaEntityTypeLookup[t] = sagaEntityType;

                var method = t.GetMethod("FindBy", new[] { messageType });

                Dictionary<Type, MethodInfo> methods;
                FinderTypeToMessageToMethodInfoLookup.TryGetValue(t, out methods);

                if (methods == null)
                {
                    methods = new Dictionary<Type, MethodInfo>();
                    FinderTypeToMessageToMethodInfoLookup[t] = methods;
                }

                methods[messageType] = method;
            }
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

        static void MapMessageTypeToSagaType(Type messageType, Type sagaType)
        {
            List<Type> sagas;
            MessageTypeToSagaTypesLookup.TryGetValue(messageType, out sagas);

            if (sagas == null)
            {
                sagas = new List<Type>(1);
                MessageTypeToSagaTypesLookup[messageType] = sagas;
            }

            if (!sagas.Contains(sagaType))
                sagas.Add(sagaType);
        }

        static void MapSagaTypeToSagaEntityType(Type sagaType, Type sagaEntityType)
        {
            SagaTypeToSagaEntityTypeLookup[sagaType] = sagaEntityType;
            SagaEntityTypeToSagaTypeLookup[sagaEntityType] = sagaType;
        }

        static void MessageTypeRequiresStartingSaga(Type messageType, Type sagaType)
        {
            List<Type> messages;
            SagaTypeToMessageTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messages);

            if (messages == null)
            {
                messages = new List<Type>(1);
                SagaTypeToMessageTypesRequiringSagaStartLookup[sagaType] = messages;
            }

            if (!messages.Contains(messageType))
                messages.Add(messageType);
        }

        static Dictionary<Type, List<Type>> MessageTypeToSagaTypesLookup = new Dictionary<Type, List<Type>>();
        static Dictionary<Type, Type> SagaEntityTypeToSagaTypeLookup = new Dictionary<Type, Type>();
        static Dictionary<Type, Type> SagaTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();
        static Dictionary<Type, Type> FinderTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();
        static Dictionary<Type, Dictionary<Type, MethodInfo>> FinderTypeToMessageToMethodInfoLookup = new Dictionary<Type, Dictionary<Type, MethodInfo>>();
        static Dictionary<Type, List<Type>> SagaTypeToMessageTypesRequiringSagaStartLookup = new Dictionary<Type, List<Type>>();
        static IConfigureHowToFindSagaWithMessage SagaMessageFindingConfiguration = new ConfigureHowToFindSagaWithMessageDispatcher();
        Conventions conventions;

        /// <summary>
        /// Until we get rid of those statics
        /// </summary>
        public static void Clear()
        {
            MessageTypeToSagaTypesLookup.Clear();
            SagaEntityTypeToSagaTypeLookup.Clear();
            SagaTypeToSagaEntityTypeLookup.Clear();
            FinderTypeToSagaEntityTypeLookup.Clear();
            FinderTypeToMessageToMethodInfoLookup.Clear();

            SagaTypeToMessageTypesRequiringSagaStartLookup.Clear();
        }
    }
}
