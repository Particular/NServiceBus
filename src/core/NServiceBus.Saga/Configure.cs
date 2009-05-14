using System;
using System.Collections.Generic;
using NServiceBus.ObjectBuilder;
using System.Reflection;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Object that scans types and stores meta-data to be used for type lookups at runtime by sagas.
    /// </summary>
    public class Configure
    {
        #region setup

        private IConfigureComponents configurer;
        private static IBuilder builderStatic;

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
            builderStatic = builder;

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
                }

                if (IsFinderType(t))
                {
                    configurer.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                    ConfigureFinder(t);
                }
            }

            CreateAdditionalFindersAsNecessary();
        }

        /// <summary>
        /// Creates an <see cref="EmptySagaFinder{T}" /> for each saga type that doesn't have a finder configured.
        /// </summary>
        private void CreateAdditionalFindersAsNecessary()
        {
            ICollection<Type> sagaEntityTypesWithFinders = finderTypeToSagaEntityTypeLookup.Values;

            foreach(Type sagaType in sagaTypeToSagaEntityTypeLookup.Keys)
            {
                Type sagaEntityType = sagaTypeToSagaEntityTypeLookup[sagaType];

                foreach (Type interfaceType in sagaType.GetInterfaces())
                {
                    var args = interfaceType.GetGenericArguments();
                    if (args.Length > 0)
                        if (typeof(ISagaMessage).IsAssignableFrom(args[0]))
                            if (typeof(IMessageHandler<>).MakeGenericType(args[0]) == interfaceType)
                            {
                                Type isagaMessageFinderType = typeof(SagaEntityFinder<>).MakeGenericType(sagaEntityType);
                                configurer.ConfigureComponent(isagaMessageFinderType, ComponentCallModelEnum.Singlecall);
                                ConfigureFinder(isagaMessageFinderType);

                                break;
                            }
                }

                if (!sagaEntityTypesWithFinders.Contains(sagaEntityType))
                {
                    Type newFinderType = typeof(EmptySagaFinder<>).MakeGenericType(sagaEntityType);
                    configurer.ConfigureComponent(newFinderType, ComponentCallModelEnum.Singlecall);
                    ConfigureFinder(newFinderType);
                }
            }
        }

        #endregion

        #region methods used at runtime

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
            finderTypeToSagaEntityTypeLookup.TryGetValue(finder.GetType(), out sagaEntityType);

            if (sagaEntityType == null)
                return null;

            Type sagaType;
            sagaEntityTypeToSagaTypeLookup.TryGetValue(sagaEntityType, out sagaType);

            if (sagaType == null)
                return null;

            List<Type> messageTypes;
            sagaTypeToMessagTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messageTypes);

            if (messageTypes == null)
                return null;

            if (messageTypes.Contains(message.GetType()))
                return sagaType;
            else
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
            List<Type> sagas = new List<Type>();
            
            foreach(Type msgTypeHandled in messageTypeToSagaTypesLookup.Keys)
                if (msgTypeHandled.IsAssignableFrom(messageType))
                    sagas.AddRange(messageTypeToSagaTypesLookup[msgTypeHandled]);

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
            sagaEntityTypeToSagaTypeLookup.TryGetValue(sagaEntityType, out result);

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
            sagaTypeToSagaEntityTypeLookup.TryGetValue(sagaType, out result);

            return result;
        }

        /// <summary>
        /// Indicates if a saga has been configured to handle the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public static bool IsMessageTypeHandledBySaga(Type messageType)
        {
            if (messageTypeToSagaTypesLookup.Keys.Contains(messageType))
                return true;

            foreach(Type msgHandledBySaga in messageTypeToSagaTypesLookup.Keys)
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
            finderTypeToMessageToMethodInfoLookup.TryGetValue(finder.GetType(), out methods);

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
            List<IFinder> result = new List<IFinder>();

            foreach(Type finderType in finderTypeToMessageToMethodInfoLookup.Keys)
            {
                IDictionary<Type, MethodInfo> messageToMethodInfo = finderTypeToMessageToMethodInfoLookup[finderType];
                if (messageToMethodInfo.ContainsKey(m.GetType()))
                {
                    result.Add(builderStatic.Build(finderType) as IFinder);
                    continue;
                }

                foreach(Type messageType in messageToMethodInfo.Keys)
                    if (messageType.IsAssignableFrom(m.GetType()))
                        result.Add(builderStatic.Build(finderType) as IFinder);
            }

            return result;
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
            sagaTypeToHandleMethodLookup.TryGetValue(saga.GetType(), out lookup);

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

                finderTypeToSagaEntityTypeLookup[t] = sagaEntityType;

                MethodInfo method = t.GetMethod("FindBy", new Type[] { messageType });

                IDictionary<Type, MethodInfo> methods;
                finderTypeToMessageToMethodInfoLookup.TryGetValue(t, out methods);

                if (methods == null)
                {
                    methods = new Dictionary<Type, MethodInfo>();
                    finderTypeToMessageToMethodInfoLookup[t] = methods;
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
            messageTypeToSagaTypesLookup.TryGetValue(messageType, out sagas);

            if (sagas == null)
            {
                sagas = new List<Type>(1);
                messageTypeToSagaTypesLookup[messageType] = sagas;
            }

            if (!sagas.Contains(sagaType))
                sagas.Add(sagaType);

            IDictionary<Type, MethodInfo> methods;
            sagaTypeToHandleMethodLookup.TryGetValue(sagaType, out methods);

            if (methods == null)
            {
                methods = new Dictionary<Type, MethodInfo>();
                sagaTypeToHandleMethodLookup[sagaType] = methods;
            }

            MethodInfo handleMethod = sagaType.GetMethod("Handle", new Type[] { messageType });
            methods[messageType] = handleMethod;
        }

        private static void MapSagaTypeToSagaEntityType(Type sagaType, Type sagaEntityType)
        {
            sagaTypeToSagaEntityTypeLookup[sagaType] = sagaEntityType;
            sagaEntityTypeToSagaTypeLookup[sagaEntityType] = sagaType;
        }

        private static void MessageTypeRequiresStartingSaga(Type messageType, Type sagaType)
        {
            List<Type> messages;
            sagaTypeToMessagTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messages);

            if (messages == null)
            {
                messages = new List<Type>(1);
                sagaTypeToMessagTypesRequiringSagaStartLookup[sagaType] = messages;
            }

            if (!messages.Contains(messageType))
                messages.Add(messageType);
        }

        #endregion

        #region members

        private readonly static IDictionary<Type, List<Type>> messageTypeToSagaTypesLookup = new Dictionary<Type, List<Type>>();
        private readonly static IDictionary<Type, IDictionary<Type, MethodInfo>> sagaTypeToHandleMethodLookup = new Dictionary<Type, IDictionary<Type, MethodInfo>>();

        private static readonly IDictionary<Type, Type> sagaEntityTypeToSagaTypeLookup = new Dictionary<Type, Type>();
        private static readonly IDictionary<Type, Type> sagaTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();

        private static readonly IDictionary<Type, Type> finderTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();
        private static readonly IDictionary<Type, IDictionary<Type, MethodInfo>> finderTypeToMessageToMethodInfoLookup = new Dictionary<Type, IDictionary<Type, MethodInfo>>();

        private static readonly IDictionary<Type, List<Type>> sagaTypeToMessagTypesRequiringSagaStartLookup = new Dictionary<Type, List<Type>>();

#endregion
    }
}
