namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Saga;

    class SagaConfigurationCache
    {
        public void ConfigureHowToFindSagaWithMessage(Type sagaType, Type messageType, SagaToMessageMap sagaToMessageMap)
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

        /// <summary>
        ///     Gets a reference to the generic "FindBy" method of the given finder
        ///     for the given message type using a hashtable lookup rather than reflection.
        /// </summary>
        public MethodInfo GetFindByMethodForFinder(IFinder finder, object message)
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
        ///     Returns a list of finder object capable of using the given message.
        /// </summary>
        public IEnumerable<Type> GetFindersForMessageAndEntity(Type messageType, Type entityType)
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
        ///     Returns the entity type configured for the given saga type.
        /// </summary>
        public Type GetSagaEntityTypeForSagaType(Type sagaType)
        {
            Type result;
            SagaTypeToSagaEntityTypeLookup.TryGetValue(sagaType, out result);

            return result;
        }

        /// <summary>
        ///     True if the given message are configure to start the saga
        /// </summary>
        public bool IsAStartSagaMessage(Type sagaType, Type messageType)
        {
            List<Type> messageTypes;
            SagaTypeToMessageTypesRequiringSagaStartLookup.TryGetValue(sagaType, out messageTypes);

            if (messageTypes == null)
            {
                return false;
            }

            if (messageTypes.Contains(messageType))
            {
                return true;
            }

            return messageTypes.Any(msgTypeHandleBySaga => msgTypeHandleBySaga.IsAssignableFrom(messageType));
        }

        public readonly Dictionary<Type, Dictionary<Type, SagaToMessageMap>> SagaEntityToMessageToPropertyLookup = new Dictionary<Type, Dictionary<Type, SagaToMessageMap>>();
        public readonly Dictionary<Type, Dictionary<Type, MethodInfo>> FinderTypeToMessageToMethodInfoLookup = new Dictionary<Type, Dictionary<Type, MethodInfo>>();
        public readonly Dictionary<Type, List<Type>> MessageTypeToSagaTypesLookup = new Dictionary<Type, List<Type>>();
        public readonly Dictionary<Type, List<Type>> SagaTypeToMessageTypesRequiringSagaStartLookup = new Dictionary<Type, List<Type>>();
        public readonly Dictionary<Type, Type> SagaTypeToSagaEntityTypeLookup = new Dictionary<Type, Type>();
    }
}