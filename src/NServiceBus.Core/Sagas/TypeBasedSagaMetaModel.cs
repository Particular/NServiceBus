namespace NServiceBus.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NServiceBus.Utils.Reflection;

    class TypeBasedSagaMetaModel
    {
        public static IEnumerable<SagaMetadata> Create(IList<Type> availableTypes, Conventions conventions)
        {
            return availableTypes.Where(IsSagaType)
                .Select(t => Create(t, availableTypes, conventions)).ToList();
        }

        static bool IsSagaType(Type t)
        {
            return SagaType.IsAssignableFrom(t) && t != SagaType && !t.IsGenericType;
        }

        static Type SagaType = typeof(Saga);

        static Type GetBaseSagaType(Type t)
        {
            var currentType = t.BaseType;
            var previousType = t;

            while (currentType != null)
            {
                if (currentType == SagaType)
                {
                    return previousType;
                }

                previousType = currentType;
                currentType = currentType.BaseType;
            }

            throw new InvalidOperationException();
        }

        public static SagaMetadata Create(Type sagaType)
        {
            return Create(sagaType, new List<Type>(), new Conventions());
        }

        public static SagaMetadata Create(Type sagaType, IEnumerable<Type> availableTypes, Conventions conventions)
        {
            if (!IsSagaType(sagaType))
            {
                throw new Exception(sagaType.FullName + " is not a saga");
            }

            var genericArguments = GetBaseSagaType(sagaType).GetGenericArguments();
            if (genericArguments.Length != 1)
            {
                throw new Exception(string.Format("'{0}' saga type does not implement Saga<T>", sagaType));
            }

            var saga = (Saga)FormatterServices.GetUninitializedObject(sagaType);
            var mapper = new SagaMapper();
            saga.ConfigureHowToFindSaga(mapper);

            var sagaEntityType = genericArguments.Single();

            ApplyScannedFinders(mapper, sagaEntityType, availableTypes, conventions);

            var correlationProperties = new List<CorrelationProperty>();
            var finders = new List<SagaFinderDefinition>();

            foreach (var mapping in mapper.Mappings)
            {
                if (!mapping.HasCustomFinderMap)
                {
                    correlationProperties.Add(new CorrelationProperty(mapping.SagaPropName));
                }

                SetFinderForMessage(mapping, sagaEntityType, finders);
            }

            var potentiallyAssociatedMessages = GetPotentiallyAssociatedMessagesForValidation(sagaType);
            ValidateForCorrectUsageOfOldAndNewStyleApiCombinations(sagaType, potentiallyAssociatedMessages);
            var associatedMessages = GetAssociatedMessages(potentiallyAssociatedMessages);

            return new SagaMetadata(sagaType.FullName, sagaType, sagaEntityType.FullName, sagaEntityType, correlationProperties, associatedMessages, finders);
        }

        static void ValidateForCorrectUsageOfOldAndNewStyleApiCombinations(Type sagaEntityType, IList<SagaMessage> sagaMessages)
        {
            var groupedByMessageType = from msg in sagaMessages
            group msg by msg.MessageType
            into msgsByType
            select new
            {
                MessageType = msgsByType.Key,
                Messages = (IEnumerable<SagaMessage>)msgsByType
            };

            const string exceptionMessage = "The saga {0} implements {1} and {2} for the message type {3}. Change the saga to only implement {2}.";
            foreach (var group in groupedByMessageType)
            {
                var handlesStartedByEventWithBothOldAndNewStyle =
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.StartedByMessage) &&
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.StartedByEvent);
                if (handlesStartedByEventWithBothOldAndNewStyle)
                {
                    throw new Exception(string.Format(exceptionMessage, sagaEntityType.Name, typeof(IAmStartedByMessages<>).FullName, typeof(IAmStartedByEvents<>).FullName, group.MessageType));
                }

                var handlesStartedByMessageWithBothOldAndNewStyle =
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.StartedByMessage) &&
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.StartedByCommand);
                if (handlesStartedByMessageWithBothOldAndNewStyle)
                {
                    throw new Exception(string.Format(exceptionMessage, sagaEntityType.Name, typeof(IAmStartedByMessages<>).FullName, typeof(IAmStartedByCommands<>).FullName, group.MessageType));
                }

                var handlesEventsWithBothOldAndNewStyle = 
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.HandleMessage) && 
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.ProcessEvent);
                if (handlesEventsWithBothOldAndNewStyle)
                {
                    throw new Exception(string.Format(exceptionMessage, sagaEntityType.Name, typeof(IHandleMessages<>).FullName, typeof(IProcessEvents<>).FullName, group.MessageType));
                }

                var handlesMessagesWithBothOldAndNewStyle =
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.HandleMessage) &&
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.ProcessCommand);
                if (handlesMessagesWithBothOldAndNewStyle)
                {
                    throw new Exception(string.Format(exceptionMessage, sagaEntityType.Name, typeof(IHandleMessages<>).FullName, typeof(IProcessCommands<>).FullName, group.MessageType));
                }

                var handlesTimeoutsWithBothOldAndNewStyle =
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.HandleTimeout) &&
                    group.Messages.Any(x => x.MessageHandledBy == SagaMessageHandledBy.ProcessTimeout);
                if (handlesTimeoutsWithBothOldAndNewStyle)
                {
                    throw new Exception(string.Format(exceptionMessage, sagaEntityType.Name, typeof(IHandleTimeouts<>).FullName, typeof(IProcessTimeouts<>).FullName, group.MessageType));
                }
            }
        }

        static void ApplyScannedFinders(SagaMapper mapper, Type sagaEntityType, IEnumerable<Type> availableTypes, Conventions conventions)
        {
            var actualFinders = availableTypes.Where(t => typeof(IFinder).IsAssignableFrom(t) && t.IsClass)
                .ToList();

            foreach (var finderType in actualFinders)
            {
                foreach (var interfaceType in finderType.GetInterfaces())
                {
                    var args = interfaceType.GetGenericArguments();
                    //since we dont want to process the IFinder type
                    if (args.Length != 2)
                    {
                        continue;
                    }

                    var entityType = args[0];
                    if (entityType != sagaEntityType)
                    {
                        continue;
                    }

                    var messageType = args[1];
                    if (!conventions.IsMessageType(messageType))
                    {
                        var error = string.Format("A custom IFindSagas must target a valid message type as defined by the message conventions. Please change '{0}' to a valid message type or add it to the message conventions. Finder name '{1}'.",messageType.FullName, finderType.FullName);
                        throw new Exception(error);
                    }

                    var existingMapping = mapper.Mappings.SingleOrDefault(m => m.MessageType == messageType);
                    if (existingMapping != null)
                    {
                        var bothMappingAndFinder = string.Format("A custom IFindSagas and an existing mapping where found for message '{0}'. Please either remove the message mapping for remove the finder. Finder name '{1}'.", messageType.FullName, finderType.FullName);
                        throw new Exception(bothMappingAndFinder);
                    }
                    mapper.ConfigureCustomFinder(finderType, messageType);
                }
            }
        }

        static void SetFinderForMessage(SagaToMessageMap mapping, Type sagaEntityType, List<SagaFinderDefinition> finders)
        {
            if (mapping.HasCustomFinderMap)
            {
                finders.Add(new SagaFinderDefinition(typeof(CustomFinderAdapter<,>).MakeGenericType(sagaEntityType, mapping.MessageType), mapping.MessageType.FullName, new Dictionary<string, object>
                {
                    {"custom-finder-clr-type", mapping.CustomFinderType}
                }));

            }
            else
            {
                finders.Add(new SagaFinderDefinition(typeof(PropertySagaFinder<>).MakeGenericType(sagaEntityType), mapping.MessageType.FullName, new Dictionary<string, object>
                {
                    {"property-accessor", mapping.MessageProp},
                    {"saga-property-name", mapping.SagaPropName}
                }));
            }
        }

        static List<SagaMessage> GetPotentiallyAssociatedMessagesForValidation(Type sagaType)
        {
            // the order of filters matters!
            return GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IAmStartedByMessages<>)).Select(t => new SagaMessage(t.FullName, SagaMessageHandledBy.StartedByMessage))
                .Union(GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IAmStartedByCommands<>)).Select(messageType => new SagaMessage(messageType.FullName, SagaMessageHandledBy.StartedByCommand)))
                .Union(GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IAmStartedByEvents<>)).Select(messageType => new SagaMessage(messageType.FullName, SagaMessageHandledBy.StartedByEvent)))
                .Union(GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleMessages<>)).Select(messageType => new SagaMessage(messageType.FullName, SagaMessageHandledBy.HandleMessage)))
                .Union(GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IProcessCommands<>)).Select(messageType => new SagaMessage(messageType.FullName, SagaMessageHandledBy.ProcessCommand)))
                .Union(GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IProcessEvents<>)).Select(messageType => new SagaMessage(messageType.FullName, SagaMessageHandledBy.ProcessEvent)))
                .Union(GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleTimeouts<>)).Select(messageType => new SagaMessage(messageType.FullName, SagaMessageHandledBy.HandleTimeout)))
                .Union(GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IProcessTimeouts<>)).Select(messageType => new SagaMessage(messageType.FullName, SagaMessageHandledBy.ProcessTimeout)))
                .ToList();
        }

        static IList<SagaMessage>  GetAssociatedMessages(IEnumerable<SagaMessage> sagaMessages)
        {
            var associatedMessages = new Dictionary<string, SagaMessage>();
            foreach (var message in sagaMessages)
            {
                SagaMessage msg;
                if (!associatedMessages.TryGetValue(message.MessageType, out msg))
                {
                    associatedMessages[message.MessageType] = message;
                }
            }
            return new List<SagaMessage>(associatedMessages.Values);
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
                    yield return argument;
                }
            }
        }
        
        class SagaMapper : IConfigureHowToFindSagaWithMessage
        {
            public List<SagaToMessageMap> Mappings = new List<SagaToMessageMap>();

            void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageExpression)
            {
                var sagaProp = Reflect<TSagaEntity>.GetProperty(sagaEntityProperty, true);

                ValidateMapping<TSagaEntity, TMessage>(messageExpression, sagaProp);

                ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);
                var compiledMessageExpression = messageExpression.Compile();
                var messageFunc = new Func<object, object>(o => compiledMessageExpression((TMessage)o));

                Mappings.Add(new SagaToMessageMap
                {
                    MessageProp = messageFunc,
                    SagaPropName = sagaProp.Name,
                    MessageType = typeof(TMessage)
                });
            }

            static void ValidateMapping<TSagaEntity, TMessage>(Expression<Func<TMessage, object>> messageExpression, PropertyInfo sagaProp)
            {
                if (sagaProp.Name.ToLower() != "id")
                {
                    return;
                }

                if (messageExpression.Body.NodeType != ExpressionType.Convert)
                {
                    return;
                }

                var memberExpr = ((UnaryExpression)messageExpression.Body).Operand as MemberExpression;

                if (memberExpr == null)
                {
                    return;
                }

                var propertyInfo = memberExpr.Member as PropertyInfo;

                var message = "Message properties mapped to the saga id needs to be of type Guid, please change property {0} on message {1} to a Guid";

                if (propertyInfo != null)
                {
                    if (propertyInfo.PropertyType != typeof(Guid))
                    {
                        throw new Exception(string.Format(message, propertyInfo.Name, typeof(TMessage).Name));
                    }

                    return;
                }


                var fieldInfo = memberExpr.Member as FieldInfo;

                if (fieldInfo != null)
                {
                    if (fieldInfo.FieldType != typeof(Guid))
                    {
                        throw new Exception(string.Format(message, fieldInfo.Name, typeof(TMessage).Name));
                    }
                }
            }

            public void ConfigureCustomFinder(Type finderType, Type messageType)
            {
                Mappings.Add(new SagaToMessageMap
                {
                    MessageType = messageType,
                    CustomFinderType = finderType
                });
            }

            // ReSharper disable once UnusedParameter.Local
            void ThrowIfNotPropertyLambdaExpression<TSagaEntity>(Expression<Func<TSagaEntity, object>> expression, PropertyInfo propertyInfo)
            {
                if (propertyInfo == null)
                {
                    throw new ArgumentException(
                        String.Format(
                            "Only public properties are supported for mapping Sagas. The lambda expression provided '{0}' is not mapping to a Property!",
                            expression.Body));
                }
            }
        }

    }
}