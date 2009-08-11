using System;
using System.Collections.Generic;
using NServiceBus.ObjectBuilder;
using System.Reflection;
using NServiceBus.Saga;
using Common.Logging;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Object that scans types and stores meta-data to be used for type lookups at runtime by sagas.
    /// </summary>
    public class Configure
    {
        #region setup

        private IConfigureComponents configurer;
        private static IBuilder _builderStatic;

        private Configure()
        {
        }

        /// <summary>
        /// Starts the configuration process for the saga infrastructure.
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static Configure With(IConfigureComponents configurer, IBuilder builder)
        {
            _builderStatic = builder;

            configurer.ConfigureComponent<ReplyingToNullOriginatorDispatcher>(ComponentCallModelEnum.Singleton);

            return new Configure { configurer = configurer };
        }

        /// <summary>
        /// Scans for types relevant to the saga infrastructure.
        /// These include implementers of <see cref="ISaga" /> and <see cref="IFindSagas{T}" />.
        /// </summary>
        /// <param name="types"></param>
        public void SagasIn(IEnumerable<Type> types)
        {
            foreach (Type t in types)
            {
                if (IsSagaType(t))
                {
                    configurer.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                    ConfigureSaga(t);
                    SagasWereFound = true;
                    continue;
                }

                if (IsFinderType(t))
                {
                    configurer.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                    ConfigureFinder(t);
                    continue;
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

        private static readonly IDictionary<Type, IDictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>> SagaEntityToMessageToPropertyLookup = new Dictionary<Type, IDictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>>();

        /// <summary>
        /// Creates an <see cref="NullSagaFinder{T}" /> for each saga type that doesn't have a finder configured.
        /// </summary>
        private void CreateAdditionalFindersAsNecessary()
        {
            foreach (Type sagaEntityType in SagaEntityToMessageToPropertyLookup.Keys)
                foreach (Type messageType in SagaEntityToMessageToPropertyLookup[sagaEntityType].Keys)
                {
                    var pair = SagaEntityToMessageToPropertyLookup[sagaEntityType][messageType];
                    CreatePropertyFinder(sagaEntityType, messageType, pair.Key, pair.Value);
                }

            foreach(Type sagaType in SagaTypeToSagaEntityTypeLookup.Keys)
            {
                Type sagaEntityType = SagaTypeToSagaEntityTypeLookup[sagaType];

                Type finder = typeof(SagaEntityFinder<>).MakeGenericType(sagaEntityType);
                configurer.ConfigureComponent(finder, ComponentCallModelEnum.Singlecall);
                ConfigureFinder(finder);

                Type nullFinder = typeof (NullSagaFinder<>).MakeGenericType(sagaEntityType);
                configurer.ConfigureComponent(nullFinder, ComponentCallModelEnum.Singlecall);
                ConfigureFinder(nullFinder);

                //TODO: Refactor the above.
            }
        }

        private void CreatePropertyFinder(Type sagaEntityType, Type messageType, PropertyInfo sagaProperty, PropertyInfo messageProperty)
        {
            Type finderType = typeof(PropertySagaFinder<,>).MakeGenericType(sagaEntityType, messageType);
            
            configurer.ConfigureComponent(finderType, ComponentCallModelEnum.Singlecall)
                .ConfigureProperty("SagaProperty", sagaProperty)
                .ConfigureProperty("MessageProperty", messageProperty);

            ConfigureFinder(finderType);
        }

        #endregion

        #region methods used at runtime

        /// <summary>
        /// Returns true if a saga type was found in the types passed in to <see cref="SagasIn"/>.
        /// </summary>
        public static bool SagasWereFound { get; private set; }


        /// <summary>
        /// Gets the saga type to instantiate and invoke if an existing saga couldn't be found by
        /// the given finder using the given message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public static Type GetSagaTypeToStartIfMessageNotFoundByFinder(IMessage message, IFinder finder)
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
            SagaTypeToMessagTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messageTypes);

            if (messageTypes == null)
                return null;

            if (messageTypes.Contains(message.GetType()))
                return sagaType;
            
            foreach (Type msgTypeHandleBySaga in messageTypes)
                if (msgTypeHandleBySaga.IsAssignableFrom(message.GetType()))
                    return sagaType;

            return null;
        }

        /// <summary>
        /// Finds the types of sagas that can handle the given concrete message type.
        /// </summary>
        /// <param name="messageType">A concrete type for a message object</param>
        /// <returns>The list of saga types.</returns>
        public static List<Type> GetSagaTypesForMessageType(Type messageType)
        {
            var sagas = new List<Type>();
            
            foreach(Type msgTypeHandled in MessageTypeToSagaTypesLookup.Keys)
                if (msgTypeHandled.IsAssignableFrom(messageType))
                    sagas.AddRange(MessageTypeToSagaTypesLookup[msgTypeHandled]);

            return sagas;
        }

        /// <summary>
        /// Returns the saga type configured for the given entity type.
        /// </summary>
        /// <param name="sagaEntityType"></param>
        /// <returns></returns>
        public static Type GetSagaTypeForSagaEntityType(Type sagaEntityType)
        {
            Type result;
            SagaEntityTypeToSagaTypeLookup.TryGetValue(sagaEntityType, out result);

            return result;
        }

        /// <summary>
        /// Returns the entity type configured for the given saga type.
        /// </summary>
        /// <param name="sagaType"></param>
        /// <returns></returns>
        public static Type GetSagaEntityTypeForSagaType(Type sagaType)
        {
            Type result;
            SagaTypeToSagaEntityTypeLookup.TryGetValue(sagaType, out result);

            return result;
        }

        /// <summary>
        /// Indicates if a saga has been configured to handle the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public static bool IsMessageTypeHandledBySaga(Type messageType)
        {
            if (MessageTypeToSagaTypesLookup.Keys.Contains(messageType))
                return true;

            foreach(Type msgHandledBySaga in MessageTypeToSagaTypesLookup.Keys)
                if (msgHandledBySaga.IsAssignableFrom(messageType))
                    return true;

            return false;
        }

        /// <summary>
        /// Gets a reference to the generic "FindBy" method of the given finder
        /// for the given message type using a hashtable lookup rather than reflection.
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static MethodInfo GetFindByMethodForFinder(IFinder finder, IMessage message)
        {
            MethodInfo result = null;

            IDictionary<Type, MethodInfo> methods;
            FinderTypeToMessageToMethodInfoLookup.TryGetValue(finder.GetType(), out methods);

            if (methods != null)
            {
                methods.TryGetValue(message.GetType(), out result);

                if (result == null)
                    foreach (Type messageType in methods.Keys)
                        if (messageType.IsAssignableFrom(message.GetType()))
                            result = methods[messageType];
            }

            return result;
        }

        /// <summary>
        /// Returns a list of finder object capable of using the given message.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static IEnumerable<IFinder> GetFindersFor(IMessage m)
        {
            var result = new List<IFinder>();

            foreach(Type finderType in FinderTypeToMessageToMethodInfoLookup.Keys)
            {
                IDictionary<Type, MethodInfo> messageToMethodInfo = FinderTypeToMessageToMethodInfoLookup[finderType];
                if (messageToMethodInfo.ContainsKey(m.GetType()))
                {
                    result.Add(_builderStatic.Build(finderType) as IFinder);
                    continue;
                }

                foreach(Type messageType in messageToMethodInfo.Keys)
                    if (messageType.IsAssignableFrom(m.GetType()))
                        result.Add(_builderStatic.Build(finderType) as IFinder);
            }

            return result;
        }

        /// <summary>
        /// Returns the list of saga types configured.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetSagaDataTypes()
        {
            return SagaTypeToSagaEntityTypeLookup.Values;
        }

        /// <summary>
        /// Gets a reference to the generic "Handle" method on the given saga
        /// for the given message type using a hashtable lookup rather than reflection.
        /// </summary>
        /// <param name="saga"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static MethodInfo GetHandleMethodForSagaAndMessage(object saga, IMessage message)
        {
            IDictionary<Type, MethodInfo> lookup;
            SagaTypeToHandleMethodLookup.TryGetValue(saga.GetType(), out lookup);

            if (lookup == null)
                return null;

            foreach (Type messageType in lookup.Keys)
                if (messageType.IsAssignableFrom(message.GetType()))
                    return lookup[messageType];

            return null;
        }

        #endregion

        #region helper methods

        private static bool IsSagaType(Type t)
        {
            return IsCompatible(t, typeof(ISaga));
        }

        private static bool IsFinderType(Type t)
        {
            return IsCompatible(t, typeof(IFinder));
        }

        private static bool IsCompatible(Type t, Type source)
        {
            return source.IsAssignableFrom(t) && t != source && !t.IsAbstract && !t.IsInterface && !t.IsGenericType;
        }

        private static void ConfigureSaga(Type t)
        {
            foreach (Type messageType in GetMessageTypesHandledBySaga(t))
                MapMessageTypeToSagaType(messageType, t);

            foreach (Type messageType in GetMessageTypesThatRequireStartingTheSaga(t))
                MessageTypeRequiresStartingSaga(messageType, t);

            PropertyInfo prop = t.GetProperty("Data");
            MapSagaTypeToSagaEntityType(t, prop.PropertyType);

            if (!typeof(IConfigurable).IsAssignableFrom(t))
                return;

            var defaultConstructor = t.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor == null)
                throw new InvalidOperationException("Sagas which implement IConfigurable, like those which inherit from Saga<T>, must have a default constructor.");

            var saga =  Activator.CreateInstance(t) as ISaga;

            var p = t.GetProperty("SagaMessageFindingConfiguration", typeof(IConfigureHowToFindSagaWithMessage));
            if (p != null)
                p.SetValue(saga, SagaMessageFindingConfiguration, null);

            if (saga is IConfigurable)
                (saga as IConfigurable).Configure();
        }


        private static void ConfigureFinder(Type t)
        {
            foreach (Type interfaceType in t.GetInterfaces())
            {
                Type[] args = interfaceType.GetGenericArguments();
                if (args.Length != 2)
                    continue;

                Type sagaEntityType = null;
                Type messageType = null;
                foreach(Type typ in args)
                {
                    if (typeof (ISagaEntity).IsAssignableFrom(typ))
                        sagaEntityType = typ;

                    if (typeof (IMessage).IsAssignableFrom(typ))
                        messageType = typ;
                }

                if (sagaEntityType == null || messageType == null)
                    continue;

                Type finderType = typeof (IFindSagas<>.Using<>).MakeGenericType(sagaEntityType, messageType);
                if (!finderType.IsAssignableFrom(t))
                    continue;

                FinderTypeToSagaEntityTypeLookup[t] = sagaEntityType;

                MethodInfo method = t.GetMethod("FindBy", new[] { messageType });

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

        private static IEnumerable<Type> GetMessageTypesHandledBySaga(Type sagaType)
        {
            return GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof (IMessageHandler<>));
        }

        private static IEnumerable<Type> GetMessageTypesThatRequireStartingTheSaga(Type sagaType)
        {
            return GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(ISagaStartedBy<>));
        }

        private static IEnumerable<Type> GetMessagesCorrespondingToFilterOnSaga(Type sagaType, Type filter)
        {
            foreach (Type interfaceType in sagaType.GetInterfaces())
            {
                Type[] types = interfaceType.GetGenericArguments();
                foreach (Type arg in types)
                    if (typeof(IMessage).IsAssignableFrom(arg))
                        if (filter.MakeGenericType(arg) == interfaceType)
                            yield return arg;
            }
        }

        private static void MapMessageTypeToSagaType(Type messageType, Type sagaType)
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

            IDictionary<Type, MethodInfo> methods;
            SagaTypeToHandleMethodLookup.TryGetValue(sagaType, out methods);

            if (methods == null)
            {
                methods = new Dictionary<Type, MethodInfo>();
                SagaTypeToHandleMethodLookup[sagaType] = methods;
            }

            MethodInfo handleMethod = sagaType.GetMethod("Handle", new[] { messageType });
            methods[messageType] = handleMethod;
        }

        private static void MapSagaTypeToSagaEntityType(Type sagaType, Type sagaEntityType)
        {
            SagaTypeToSagaEntityTypeLookup[sagaType] = sagaEntityType;
            SagaEntityTypeToSagaTypeLookup[sagaEntityType] = sagaType;
        }

        private static void MessageTypeRequiresStartingSaga(Type messageType, Type sagaType)
        {
            List<Type> messages;
            SagaTypeToMessagTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messages);

            if (messages == null)
            {
                messages = new List<Type>(1);
                SagaTypeToMessagTypesRequiringSagaStartLookup[sagaType] = messages;
            }

            if (!messages.Contains(messageType))
                messages.Add(messageType);
        }

        #endregion

        #region members

        private readonly static IDictionary<Type, List<Type>> MessageTypeToSagaTypesLookup = new Dictionary<Type, List<Type>>();
        private readonly static IDictionary<Type, IDictionary<Type, MethodInfo>> SagaTypeToHandleMethodLookup = new Dictionary<Type, IDictionary<Type, MethodInfo>>();

        private static readonly IDictionary<Type, Type> SagaEntityTypeToSagaTypeLookup = new Dictionary<Type, Type>();
        private static readonly IDictionary<Type, Type> SagaTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();

        private static readonly IDictionary<Type, Type> FinderTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();
        private static readonly IDictionary<Type, IDictionary<Type, MethodInfo>> FinderTypeToMessageToMethodInfoLookup = new Dictionary<Type, IDictionary<Type, MethodInfo>>();

        private static readonly IDictionary<Type, List<Type>> SagaTypeToMessagTypesRequiringSagaStartLookup = new Dictionary<Type, List<Type>>();

        private static readonly IConfigureHowToFindSagaWithMessage SagaMessageFindingConfiguration = new ConfigureHowToFindSagaWithMessageDispatcher();

        internal static readonly ILog Logger = LogManager.GetLogger("NServiceBus");
        #endregion
    }
}
