namespace NServiceBus.Features
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
    /// Sagas
    /// </summary>
    public class Sagas : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="Sagas"/>.
        /// </summary>
        public Sagas()
        {
            EnableByDefault();
            Prerequisite(config => config.Settings.GetAvailableTypes().Any(IsSagaType));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            foreach (var t in context.Settings.GetAvailableTypes())
            {
                if (IsSagaType(t))
                {
                    Configure.Component(t, DependencyLifecycle.InstancePerCall);
                    ConfigureSaga(t);
                    continue;
                }

                if (IsFinderType(t))
                {
                    Configure.Component(t, DependencyLifecycle.InstancePerCall);
                    ConfigureFinder(t);
                    continue;
                }

                if (IsSagaNotFoundHandler(t))
                {
                    Configure.Component(t, DependencyLifecycle.InstancePerCall);
                }
            }

            CreateAdditionalFindersAsNecessary();
        }


        internal static void ConfigureHowToFindSagaWithMessage(Type sagaType, PropertyInfo sagaProp, Type messageType, PropertyInfo messageProp)
        {
            IDictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>> messageToProperties;
            SagaEntityToMessageToPropertyLookup.TryGetValue(sagaType, out messageToProperties);

            if (messageToProperties == null)
            {
                messageToProperties = new Dictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>();
                SagaEntityToMessageToPropertyLookup[sagaType] = messageToProperties;
            }

            messageToProperties[messageType] = new KeyValuePair<PropertyInfo, PropertyInfo>(sagaProp, messageProp);
        }

        public static readonly IDictionary<Type, IDictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>> SagaEntityToMessageToPropertyLookup = new Dictionary<Type, IDictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>>();

        void CreateAdditionalFindersAsNecessary()
        {
            foreach (var sagaEntityPair in SagaEntityToMessageToPropertyLookup)
            {
                foreach (var messageType in sagaEntityPair.Value.Keys)
                {
                    var pair = sagaEntityPair.Value[messageType];
                    CreatePropertyFinder(sagaEntityPair.Key, messageType, pair.Key, pair.Value);
                }
            }

            foreach (var sagaEntityType in SagaTypeToSagaEntityTypeLookup.Values)
            {
                var sagaHeaderIdFinder = typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType);
                Configure.Component(sagaHeaderIdFinder, DependencyLifecycle.InstancePerCall);
                ConfigureFinder(sagaHeaderIdFinder);
            }
        }

        void CreatePropertyFinder(Type sagaEntityType, Type messageType, PropertyInfo sagaProperty, PropertyInfo messageProperty)
        {
            var finderType = typeof(PropertySagaFinder<,>).MakeGenericType(sagaEntityType, messageType);

            Configure.Component(finderType, DependencyLifecycle.InstancePerCall)
                .ConfigureProperty("SagaProperty", sagaProperty)
                .ConfigureProperty("MessageProperty", messageProperty);

            ConfigureFinder(finderType);
        }

        /// <summary>
        /// True if the given message are configure to start the saga
        /// </summary>
        public static bool ShouldMessageStartSaga(Type sagaType, Type messageType)
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
        public static Type GetSagaTypeForSagaEntityType(Type sagaEntityType)
        {
            Type result;
            SagaEntityTypeToSagaTypeLookup.TryGetValue(sagaEntityType, out result);

            return result;
        }

        /// <summary>
        /// Returns the entity type configured for the given saga type.
        /// </summary>
        public static Type GetSagaEntityTypeForSagaType(Type sagaType)
        {
            Type result;
            SagaTypeToSagaEntityTypeLookup.TryGetValue(sagaType, out result);

            return result;
        }

        /// <summary>
        /// Gets a reference to the generic "FindBy" method of the given finder
        /// for the given message type using a hashtable lookup rather than reflection.
        /// </summary>
        public static MethodInfo GetFindByMethodForFinder(IFinder finder, object message)
        {
            MethodInfo result = null;

            IDictionary<Type, MethodInfo> methods;
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
        public static IEnumerable<Type> GetFindersForMessageAndEntity(Type messageType, Type entityType)
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
        public static IEnumerable<Type> GetSagaDataTypes()
        {
            return SagaTypeToSagaEntityTypeLookup.Values;
        }

        public static bool IsSagaType(Type t)
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

        public static void ConfigureSaga(Type t)
        {
            foreach (var messageType in GetMessageTypesHandledBySaga(t))
                MapMessageTypeToSagaType(messageType, t);

            foreach (var messageType in GetMessageTypesThatRequireStartingTheSaga(t))
                MessageTypeRequiresStartingSaga(messageType, t);

            var prop = t.GetProperty("Data");
            MapSagaTypeToSagaEntityType(t, prop.PropertyType);

            var saga = (Saga)FormatterServices.GetUninitializedObject(t);
            saga.ConfigureHowToFindSaga(SagaMessageFindingConfiguration);
        }


        public static void ConfigureFinder(Type t)
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

                    if (MessageConventionExtensions.IsMessageType(type) || type == typeof(object))
                        messageType = type;
                }

                if (sagaEntityType == null || messageType == null)
                    continue;

                var finderType = typeof(IFindSagas<>.Using<>).MakeGenericType(sagaEntityType, messageType);
                if (!finderType.IsAssignableFrom(t))
                    continue;

                FinderTypeToSagaEntityTypeLookup[t] = sagaEntityType;

                var method = t.GetMethod("FindBy", new[] { messageType });

                IDictionary<Type, MethodInfo> methods;
                FinderTypeToMessageToMethodInfoLookup.TryGetValue(t, out methods);

                if (methods == null)
                {
                    methods = new Dictionary<Type, MethodInfo>();
                    FinderTypeToMessageToMethodInfoLookup[t] = methods;
                }

                methods[messageType] = method;
            }
        }

        internal static IEnumerable<Type> GetMessageTypesHandledBySaga(Type sagaType)
        {
            return GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleMessages<>));
        }

        internal static IEnumerable<Type> GetMessageTypesThatRequireStartingTheSaga(Type sagaType)
        {
            return GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IAmStartedByMessages<>));
        }

        static IEnumerable<Type> GetMessagesCorrespondingToFilterOnSaga(Type sagaType, Type filter)
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
                    if (MessageConventionExtensions.IsMessageType(argument))
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

        readonly static IDictionary<Type, List<Type>> MessageTypeToSagaTypesLookup = new Dictionary<Type, List<Type>>();

        static readonly IDictionary<Type, Type> SagaEntityTypeToSagaTypeLookup = new Dictionary<Type, Type>();
        static readonly IDictionary<Type, Type> SagaTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();

        static readonly IDictionary<Type, Type> FinderTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();
        static readonly IDictionary<Type, IDictionary<Type, MethodInfo>> FinderTypeToMessageToMethodInfoLookup = new Dictionary<Type, IDictionary<Type, MethodInfo>>();

        static readonly IDictionary<Type, List<Type>> SagaTypeToMessageTypesRequiringSagaStartLookup = new Dictionary<Type, List<Type>>();

        static readonly IConfigureHowToFindSagaWithMessage SagaMessageFindingConfiguration = new ConfigureHowToFindSagaWithMessageDispatcher();

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