using System;
using System.Collections.Generic;
using ObjectBuilder;
using System.Reflection;

namespace NServiceBus.Saga
{
    public class Configure
    {
        #region setup

        private IBuilder builder;

        private Configure()
        {
        }

        public static Configure With(IBuilder builder)
        {
            Configure c = new Configure();
            c.builder = builder;

            builder.ConfigureComponent(typeof (SagaEntityFinder), ComponentCallModelEnum.Singleton);

            return c;
        }

        public void SagasInAssemblies(params Assembly[] assemblies)
        {
            foreach (Assembly a in assemblies)
                foreach (Type t in a.GetTypes())
                {
                    if (IsSagaType(t))
                    {
                        builder.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                        ConfigureSaga(t);
                    }

                    if (IsFinderType(t))
                    {
                        builder.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                        ConfigureFinder(t);
                    }
                }

            CreateAdditionalFindersAsNecessary();
        }

        private void CreateAdditionalFindersAsNecessary()
        {
            ICollection<Type> sagaEntityTypesWithFinders = finderTypeToSagaEntityTypeLookup.Values;

            foreach(Type sagaType in sagaTypeToSagaEntityTypeLookup.Keys)
            {
                Type sagaEntityType = sagaTypeToSagaEntityTypeLookup[sagaType];
                if (sagaEntityTypesWithFinders.Contains(sagaEntityType))
                    continue;

                Type newFinderType = typeof (EmptySagaFinder<>).MakeGenericType(sagaEntityType);
                builder.ConfigureComponent(newFinderType, ComponentCallModelEnum.Singlecall);
                ConfigureFinder(newFinderType);
            }
        }

        #endregion

        #region methods used at runtime

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

        public static MethodInfo GetFindByMethodForFinder(IFinder finder)
        {
            MethodInfo result;
            finderTypeToMethodInfoLookup.TryGetValue(finder.GetType(), out result);

            return result;
        }

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
                if (args.Length != 1)
                    continue;

                if (!typeof (ISagaEntity).IsAssignableFrom(args[0]) && args[0] != typeof(ISagaEntity))
                    continue;

                Type finderType = typeof (IFindSagas<>).MakeGenericType(args[0]);
                if (!finderType.IsAssignableFrom(t))
                    continue;

                finderTypeToSagaEntityTypeLookup[t] = args[0];

                MethodInfo method = t.GetMethod("FindBy", new Type[] { typeof(IMessage) });
                finderTypeToMethodInfoLookup[t] = method;
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
        private static readonly IDictionary<Type, MethodInfo> finderTypeToMethodInfoLookup = new Dictionary<Type, MethodInfo>();

        private static readonly IDictionary<Type, List<Type>> sagaTypeToMessagTypesRequiringSagaStartLookup = new Dictionary<Type, List<Type>>();

#endregion
    }
}
