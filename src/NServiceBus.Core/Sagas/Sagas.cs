namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Config;
    using Logging;
    using NServiceBus.Sagas;
    using NServiceBus.Sagas.Finders;
    using Saga;

    public class Sagas : Feature
    {
        public override void Initialize()
        {
            Configure.Component<ReplyingToNullOriginatorDispatcher>(DependencyLifecycle.SingleInstance);

            var sagasFound = FindAndConfigureSagasIn(Configure.TypesToScan);

            if (sagasFound)
            {
                InfrastructureServices.Enable<ISagaPersister>();

                Logger.InfoFormat("Sagas found in scanned types, saga persister enabled");
            }
            else
            {
                Logger.InfoFormat("The saga feature was enabled but no saga implementations could be found. No need to enable the configured saga persister");
            }
        }

        /// <summary>
        /// Scans for types relevant to the saga infrastructure.
        /// These include implementers of <see cref="ISaga" /> and <see cref="IFindSagas{T}" />.
        /// </summary>
        public bool FindAndConfigureSagasIn(IEnumerable<Type> types)
        {
            var sagasWereFound = false;

            foreach (var t in types)
            {
                if (IsSagaType(t))
                {
                    Configure.Component(t, DependencyLifecycle.InstancePerCall);
                    ConfigureSaga(t);
                    sagasWereFound = true;
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
                    continue;
                }
            }

            CreateAdditionalFindersAsNecessary();

            return sagasWereFound;
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

        /// <summary>
        /// Creates an <see cref="NullSagaFinder{T}" /> for each saga type that doesn't have a finder configured.
        /// </summary>
        private void CreateAdditionalFindersAsNecessary()
        {
            foreach (var sagaEntityType in SagaEntityToMessageToPropertyLookup.Keys)
                foreach (var messageType in SagaEntityToMessageToPropertyLookup[sagaEntityType].Keys)
                {
                    var pair = SagaEntityToMessageToPropertyLookup[sagaEntityType][messageType];
                    CreatePropertyFinder(sagaEntityType, messageType, pair.Key, pair.Value);
                }

            foreach (var sagaType in SagaTypeToSagaEntityTypeLookup.Keys)
            {
                var sagaEntityType = SagaTypeToSagaEntityTypeLookup[sagaType];


                var nullFinder = typeof(NullSagaFinder<>).MakeGenericType(sagaEntityType);
                Configure.Component(nullFinder, DependencyLifecycle.InstancePerCall);
                ConfigureFinder(nullFinder);

                var sagaHeaderIdFinder = typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType);
                Configure.Component(sagaHeaderIdFinder, DependencyLifecycle.InstancePerCall);
                ConfigureFinder(sagaHeaderIdFinder);
            }
        }

        private void CreatePropertyFinder(Type sagaEntityType, Type messageType, PropertyInfo sagaProperty, PropertyInfo messageProperty)
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
        /// <param name="sagaType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool ShouldMessageStartSaga(Type sagaType, Type messageType)
        {
            List<Type> messageTypes;
            SagaTypeToMessageTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messageTypes);

            if (messageTypes == null)
                return false;

            return messageTypes.Contains(messageType);
        }

        /// <summary>
        /// Gets the saga type to instantiate and invoke if an existing saga couldn't be found by
        /// the given finder using the given message.
        /// </summary>
        public static Type GetSagaTypeToStartIfMessageNotFoundByFinder(object message, IFinder finder)
        {
            Type sagaEntityType;
            FinderTypeToSagaEntityTypeLookup.TryGetValue(finder.GetType(), out sagaEntityType);

            if (sagaEntityType == null)
                return null;

            Type sagaType;
            SagaEntityTypeToSagaTypeLookup.TryGetValue(sagaEntityType, out sagaType);

            if (sagaType == null)
                return null;

            List<Type> messageTypes;
            SagaTypeToMessageTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messageTypes);

            if (messageTypes == null)
                return null;

            if (messageTypes.Contains(message.GetType()))
                return sagaType;

            foreach (var msgTypeHandleBySaga in messageTypes)
                if (msgTypeHandleBySaga.IsInstanceOfType(message))
                    return sagaType;

            return null;
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
                    foreach (var messageType in methods.Keys)
                        if (messageType.IsInstanceOfType(message))
                            result = methods[messageType];
            }

            return result;
        }

        /// <summary>
        /// Returns a list of finder object capable of using the given message.
        /// </summary>
        [ObsoleteEx(Replacement = "GetFindersForMessageAndEntity", TreatAsErrorFromVersion = "4.3")]
        public static IEnumerable<Type> GetFindersFor(object m)
        {
            foreach (var finderType in FinderTypeToMessageToMethodInfoLookup.Keys)
            {
                var messageToMethodInfo = FinderTypeToMessageToMethodInfoLookup[finderType];
                if (messageToMethodInfo.ContainsKey(m.GetType()))
                {
                    yield return finderType;
                    continue;
                }

                foreach (var messageType in messageToMethodInfo.Keys)
                    if (messageType.IsInstanceOfType(m))
                        yield return finderType;
            }
        }

        /// <summary>
        /// Returns a list of finder object capable of using the given message.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetFindersForMessageAndEntity(Type messageType, Type entityType)
        {
            foreach (var finderType in FinderTypeToMessageToMethodInfoLookup.Keys)
            {
                var messageToMethodInfo = FinderTypeToMessageToMethodInfoLookup[finderType];

                MethodInfo methodInfo;

                if (messageToMethodInfo.TryGetValue(messageType, out methodInfo) && methodInfo.ReturnType == entityType)
                {
                    yield return finderType;
                }
            }
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
            return IsCompatible(t, typeof(ISaga));
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

            if (!typeof(IConfigurable).IsAssignableFrom(t))
                return;

            var defaultConstructor = t.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor == null)
                throw new InvalidOperationException("Sagas which implement IConfigurable, like those which inherit from Saga<T>, must have a default constructor.");

            var saga = Activator.CreateInstance(t) as ISaga;

            var p = t.GetProperty("SagaMessageFindingConfiguration", typeof(IConfigureHowToFindSagaWithMessage));
            if (p != null)
                p.SetValue(saga, SagaMessageFindingConfiguration, null);

            if (saga is IConfigurable)
                (saga as IConfigurable).Configure();
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

        static readonly ILog Logger = LogManager.GetLogger(typeof(ReplyingToNullOriginatorDispatcher));

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