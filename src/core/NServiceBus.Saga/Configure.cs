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

                        foreach(Type messageType in GetMessageTypesHandledBySaga(t))
                            MapMessageTypeToSagaType(messageType, t);

                        foreach(Type messageType in GetMessageTypesThatRequireStartingTheSaga(t))
                            MessageTypeRequiresStartingSaga(messageType, t);

                        PropertyInfo prop = t.GetProperty("Data");
                        MapSagaTypeToSagaEntityType(t, prop.PropertyType);
                    }

                    if (IsFinderType(t))
                    {
                        builder.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
                        ConfigureFinder(t);
                    }
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
            return messageTypeToSagaTypesLookup.Keys.Contains(messageType);
        }

        #endregion

        #region helper methods

        private static bool IsSagaType(Type t)
        {
            return typeof (ISaga).IsAssignableFrom(t) && t != typeof (ISaga) && !t.IsAbstract && !t.IsInterface;
        }

        private static bool IsFinderType(Type t)
        {
            return typeof(IFinder).IsAssignableFrom(t) && t != typeof(IFinder) && !t.IsInterface && !t.IsAbstract;
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

            sagas.Add(sagaType);
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

            messages.Add(messageType);
        }

        #endregion

        #region members

        private readonly static IDictionary<Type, List<Type>> messageTypeToSagaTypesLookup = new Dictionary<Type, List<Type>>();

        private static readonly IDictionary<Type, Type> sagaEntityTypeToSagaTypeLookup = new Dictionary<Type, Type>();
        private static readonly IDictionary<Type, Type> sagaTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();

        private static readonly IDictionary<Type, Type> finderTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();

        private static readonly IDictionary<Type, List<Type>> sagaTypeToMessagTypesRequiringSagaStartLookup = new Dictionary<Type, List<Type>>();

#endregion
    }
}
