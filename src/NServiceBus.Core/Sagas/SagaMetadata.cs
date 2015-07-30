namespace NServiceBus.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    ///     Contains metadata for known sagas.
    /// </summary>
    public class SagaMetadata
    {
        Dictionary<string, SagaMessage> associatedMessages;
        Dictionary<string, SagaFinderDefinition> sagaFinders;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SagaMetadata" /> class.
        /// </summary>
        /// <param name="name">The name of the saga.</param>
        /// <param name="sagaType">The type for this saga.</param>
        /// <param name="entityName">The name of the saga data entity.</param>
        /// <param name="sagaEntityType">The type of the related saga entity.</param>
        /// <param name="correlationProperties">Properties this saga is correlated on.</param>
        /// <param name="messages">The messages collection that a saga handles.</param>
        /// <param name="finders">The finder definition collection that can find this saga.</param>
        public SagaMetadata(string name, Type sagaType, string entityName, Type sagaEntityType, List<CorrelationProperty> correlationProperties, IEnumerable<SagaMessage> messages, IEnumerable<SagaFinderDefinition> finders)
        {
            Name = name;
            EntityName = entityName;
            SagaEntityType = sagaEntityType;
            SagaType = sagaType;
            CorrelationProperties = correlationProperties;

            associatedMessages = new Dictionary<string, SagaMessage>();

            foreach (var sagaMessage in messages)
            {
                associatedMessages[sagaMessage.MessageType] = sagaMessage;
            }

            sagaFinders = new Dictionary<string, SagaFinderDefinition>();

            foreach (var finder in finders)
            {
                sagaFinders[finder.MessageType] = finder;
            }
        }

        internal static bool IsSagaType(Type t)
        {
            return typeof(Saga).IsAssignableFrom(t) && t != typeof(Saga) && !t.IsGenericType;
        }

        /// <summary>
        ///     Returns the list of messages that is associated with this saga.
        /// </summary>
        public IEnumerable<SagaMessage> AssociatedMessages
        {
            get { return associatedMessages.Values; }
        }

        /// <summary>
        ///     Gets the list of finders for this saga.
        /// </summary>
        public IEnumerable<SagaFinderDefinition> Finders
        {
            get { return sagaFinders.Values; }
        }

        /// <summary>
        ///     The name of the saga.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     The name of the saga data entity.
        /// </summary>
        public string EntityName { get; private set; }

        /// <summary>
        ///     The type of the related saga entity.
        /// </summary>
        public Type SagaEntityType { get; private set; }

        /// <summary>
        ///     The type for this saga.
        /// </summary>
        public Type SagaType { get; private set; }

        /// <summary>
        ///     Properties this saga is correlated on.
        /// </summary>
        public List<CorrelationProperty> CorrelationProperties { get; private set; }

        /// <summary>
        ///     True if the specified message type is allowed to start the saga.
        /// </summary>
        public bool IsMessageAllowedToStartTheSaga(string messageType)
        {
            SagaMessage sagaMessage;

            if (!associatedMessages.TryGetValue(messageType, out sagaMessage))
            {
                return false;
            }
            return sagaMessage.IsAllowedToStartSaga;
        }

        /// <summary>
        ///     Gets the configured finder for this message.
        /// </summary>
        /// <param name="messageType">The message <see cref="MemberInfo.Name"/>.</param>
        /// <param name="finderDefinition">The finder if present.</param>
        /// <returns>True if finder exists.</returns>
        public bool TryGetFinder(string messageType, out SagaFinderDefinition finderDefinition)
        {
            return sagaFinders.TryGetValue(messageType, out finderDefinition);
        }

        /// <summary>
        /// Creates a <see cref="SagaMetadata" /> from a specific Saga type.
        /// </summary>
        /// <param name="sagaType">A type representing a Saga. Must be a non-generic type inheriting from <see cref="NServiceBus.Saga.Saga" />.</param>
        /// <returns>An instance of <see cref="SagaMetadata" /> describing the Saga.</returns>
        public static SagaMetadata Create(Type sagaType)
        {
            return Create(sagaType, new List<Type>(), new Conventions());
        }

        /// <summary>
        /// Creates a <see cref="SagaMetadata" /> from a specific Saga type.
        /// </summary>
        /// <param name="sagaType">A type representing a Saga. Must be a non-generic type inheriting from <see cref="NServiceBus.Saga.Saga" />.</param>
        /// <param name="availableTypes">Additional available types, used to locate saga finders and other related classes.</param>
        /// <param name="conventions">Custom conventions to use while scanning types.</param>
        /// <returns>An instance of <see cref="SagaMetadata" /> describing the Saga.</returns>
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

            var associatedMessages = GetAssociatedMessages(sagaType)
                .ToList();

            return new SagaMetadata(sagaType.FullName, sagaType, sagaEntityType.FullName, sagaEntityType, correlationProperties, associatedMessages, finders);
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
                        var error = string.Format("A custom IFindSagas must target a valid message type as defined by the message conventions. Please change '{0}' to a valid message type or add it to the message conventions. Finder name '{1}'.", messageType.FullName, finderType.FullName);
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

        static IEnumerable<SagaMessage> GetAssociatedMessages(Type sagaType)
        {
            var result = GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IAmStartedByMessages<>))
                .Select(t => new SagaMessage(t.FullName, true)).ToList();

            foreach (var messageType in GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleMessages<>)))
            {
                if (result.Any(m => m.MessageType == messageType.FullName))
                {
                    continue;
                }
                result.Add(new SagaMessage(messageType.FullName, false));
            }

            foreach (var messageType in GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleTimeouts<>)))
            {
                if (result.Any(m => m.MessageType == messageType.FullName))
                {
                    continue;
                }
                result.Add(new SagaMessage(messageType.FullName, false));
            }

            return result;
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

        static Type GetBaseSagaType(Type t)
        {
            var currentType = t.BaseType;
            var previousType = t;

            while (currentType != null)
            {
                if (currentType == typeof(Saga))
                {
                    return previousType;
                }

                previousType = currentType;
                currentType = currentType.BaseType;
            }

            throw new InvalidOperationException();
        }
    }
}