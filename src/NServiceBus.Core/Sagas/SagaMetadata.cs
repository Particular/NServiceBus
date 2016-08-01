namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Contains metadata for known sagas.
    /// </summary>
    public class SagaMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SagaMetadata" /> class.
        /// </summary>
        /// <param name="name">The name of the saga.</param>
        /// <param name="sagaType">The type for this saga.</param>
        /// <param name="entityName">The name of the saga data entity.</param>
        /// <param name="sagaEntityType">The type of the related saga entity.</param>
        /// <param name="correlationProperty">The property this saga is correlated on if any.</param>
        /// <param name="messages">The messages collection that a saga handles.</param>
        /// <param name="finders">The finder definition collection that can find this saga.</param>
        public SagaMetadata(string name, Type sagaType, string entityName, Type sagaEntityType, CorrelationPropertyMetadata correlationProperty, IReadOnlyCollection<SagaMessage> messages, IReadOnlyCollection<SagaFinderDefinition> finders)
        {
            this.correlationProperty = correlationProperty;
            Name = name;
            EntityName = entityName;
            SagaEntityType = sagaEntityType;
            SagaType = sagaType;


            if (!messages.Any(m => m.IsAllowedToStartSaga))
            {
                throw new Exception($@"
Sagas must have at least one message that is allowed to start the saga. Add at least one `IAmStartedByMessages` to the {name} saga.");
            }

            if (correlationProperty != null)
            {
                if (!AllowedCorrelationPropertyTypes.Contains(correlationProperty.Type))
                {
                    var supportedTypes = string.Join(",", AllowedCorrelationPropertyTypes.Select(t => t.Name));

                    throw new Exception($@"
{correlationProperty.Type.Name} is not supported for correlated properties. Change the correlation property {correlationProperty.Name} on saga {name} to any of the supported types, {supportedTypes}, or use a custom saga finder.");
                }
            }

            associatedMessages = new Dictionary<string, SagaMessage>();

            foreach (var sagaMessage in messages)
            {
                associatedMessages[sagaMessage.MessageTypeName] = sagaMessage;
            }

            sagaFinders = new Dictionary<string, SagaFinderDefinition>();

            foreach (var finder in finders)
            {
                sagaFinders[finder.MessageTypeName] = finder;
            }
        }

        /// <summary>
        /// Returns the list of messages that is associated with this saga.
        /// </summary>
        public IReadOnlyCollection<SagaMessage> AssociatedMessages => associatedMessages.Values.ToList();

        /// <summary>
        /// Gets the list of finders for this saga.
        /// </summary>
        public IReadOnlyCollection<SagaFinderDefinition> Finders => sagaFinders.Values.ToList();

        /// <summary>
        /// The name of the saga.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The name of the saga data entity.
        /// </summary>
        public string EntityName { get; private set; }

        /// <summary>
        /// The type of the related saga entity.
        /// </summary>
        public Type SagaEntityType { get; private set; }

        /// <summary>
        /// The type for this saga.
        /// </summary>
        public Type SagaType { get; private set; }

        /// <summary>
        /// Property this saga is correlated on.
        /// </summary>
        public bool TryGetCorrelationProperty(out CorrelationPropertyMetadata property)
        {
            property = correlationProperty;

            return property != null;
        }

        internal static bool IsSagaType(Type t)
        {
            return typeof(Saga).IsAssignableFrom(t) && t != typeof(Saga) && !t.IsGenericType && !t.IsAbstract;
        }

        /// <summary>
        /// True if the specified message type is allowed to start the saga.
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
        /// Gets the configured finder for this message.
        /// </summary>
        /// <param name="messageType">The message <see cref="MemberInfo.Name" />.</param>
        /// <param name="finderDefinition">The finder if present.</param>
        /// <returns>True if finder exists.</returns>
        public bool TryGetFinder(string messageType, out SagaFinderDefinition finderDefinition)
        {
            return sagaFinders.TryGetValue(messageType, out finderDefinition);
        }

        /// <summary>
        /// Creates a <see cref="SagaMetadata" /> from a specific Saga type.
        /// </summary>
        /// <param name="sagaType">A type representing a Saga. Must be a non-generic type inheriting from <see cref="Saga" />.</param>
        /// <returns>An instance of <see cref="SagaMetadata" /> describing the Saga.</returns>
        public static SagaMetadata Create(Type sagaType)
        {
            return Create(sagaType, new List<Type>(), new Conventions());
        }

        /// <summary>
        /// Creates a <see cref="SagaMetadata" /> from a specific Saga type.
        /// </summary>
        /// <param name="sagaType">A type representing a Saga. Must be a non-generic type inheriting from <see cref="Saga" />.</param>
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
                throw new Exception($"'{sagaType.Name}' saga type does not implement Saga<T>");
            }

            var saga = (Saga) FormatterServices.GetUninitializedObject(sagaType);
            var mapper = new SagaMapper();
            saga.ConfigureHowToFindSaga(mapper);

            var sagaEntityType = genericArguments.Single();

            ApplyScannedFinders(mapper, sagaEntityType, availableTypes, conventions);

            var finders = new List<SagaFinderDefinition>();


            var propertyMappings = mapper.Mappings.Where(m => !m.HasCustomFinderMap)
                .GroupBy(m => m.SagaPropName)
                .ToList();

            if (propertyMappings.Count > 1)
            {
                var messageTypes = string.Join(",", propertyMappings.SelectMany(g => g.Select(m => m.MessageType.FullName)).Distinct());
                throw new Exception($"Sagas can only have mappings that correlate on a single saga property. Use custom finders to correlate {messageTypes} to saga {sagaType.Name}");
            }

            CorrelationPropertyMetadata correlationProperty = null;

            if (propertyMappings.Any())
            {
                var mapping = propertyMappings.Single().First();
                correlationProperty = new CorrelationPropertyMetadata(mapping.SagaPropName, mapping.SagaPropType);
            }

            var associatedMessages = GetAssociatedMessages(sagaType)
                .ToList();

            foreach (var mapping in mapper.Mappings)
            {
                var associatedMessage = associatedMessages.FirstOrDefault(m => mapping.MessageType.IsAssignableFrom(m.MessageType));
                if (associatedMessage == null)
                {
                    var msgType = mapping.MessageType.Name;
                    if (mapping.HasCustomFinderMap)
                    {
                        throw new Exception($"Custom saga finder {mapping.CustomFinderType.FullName} maps message type {msgType} for saga {sagaType.Name}, but the saga does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.");
                    }
                    throw new Exception($"Saga {sagaType.Name} contains a mapping for {msgType} in the ConfigureHowToFindSaga method, but does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.");
                }
                SetFinderForMessage(mapping, sagaEntityType, finders);
            }

            foreach (var messageType in associatedMessages)
            {
                if (messageType.IsAllowedToStartSaga)
                {
                    var match = mapper.Mappings.FirstOrDefault(m => m.MessageType.IsAssignableFrom(messageType.MessageType));
                    if (match == null)
                    {
                        var simpleName = messageType.MessageType.Name;
                        throw new Exception($"Message type {simpleName} can start the saga {sagaType.Name} (the saga implements IAmStartedByMessages<{simpleName}>) but does not map that message to saga data. In the ConfigureHowToFindSaga method, add a mapping using:{Environment.NewLine}    mapper.ConfigureMapping<{simpleName}>(message => message.SomeMessageProperty).ToSaga(saga => saga.MatchingSagaProperty);");
                    }
                }
            }

            return new SagaMetadata(sagaType.FullName, sagaType, sagaEntityType.FullName, sagaEntityType, correlationProperty, associatedMessages, finders);
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
                    //since we don't want to process the IFinder type
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
                        var error = $"A custom IFindSagas must target a valid message type as defined by the message conventions. Change '{messageType.FullName}' to a valid message type or add it to the message conventions. Finder name '{finderType.FullName}'.";
                        throw new Exception(error);
                    }

                    var existingMapping = mapper.Mappings.SingleOrDefault(m => m.MessageType == messageType);
                    if (existingMapping != null)
                    {
                        var bothMappingAndFinder = $"A custom IFindSagas and an existing mapping where found for message '{messageType.FullName}'. Either remove the message mapping for remove the finder. Finder name '{finderType.FullName}'.";
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
                finders.Add(new SagaFinderDefinition(typeof(CustomFinderAdapter<,>).MakeGenericType(sagaEntityType, mapping.MessageType), mapping.MessageType, new Dictionary<string, object>
                {
                    {"custom-finder-clr-type", mapping.CustomFinderType}
                }));
            }
            else
            {
                finders.Add(new SagaFinderDefinition(typeof(PropertySagaFinder<>).MakeGenericType(sagaEntityType), mapping.MessageType, new Dictionary<string, object>
                {
                    {"property-accessor", mapping.MessageProp},
                    {"saga-property-name", mapping.SagaPropName}
                }));
            }
        }

        static IEnumerable<SagaMessage> GetAssociatedMessages(Type sagaType)
        {
            var result = GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IAmStartedByMessages<>))
                .Select(t => new SagaMessage(t, true)).ToList();

            foreach (var messageType in GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleMessages<>)))
            {
                if (result.Any(m => m.MessageType == messageType))
                {
                    continue;
                }
                result.Add(new SagaMessage(messageType, false));
            }

            foreach (var messageType in GetMessagesCorrespondingToFilterOnSaga(sagaType, typeof(IHandleTimeouts<>)))
            {
                if (result.Any(m => m.MessageType == messageType))
                {
                    continue;
                }
                result.Add(new SagaMessage(messageType, false));
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

        Dictionary<string, SagaMessage> associatedMessages;
        CorrelationPropertyMetadata correlationProperty;
        Dictionary<string, SagaFinderDefinition> sagaFinders;

        static Type[] AllowedCorrelationPropertyTypes =
        {
            typeof(Guid),
            typeof(string),
            typeof(long),
            typeof(ulong),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort)
        };

        class SagaMapper : IConfigureHowToFindSagaWithMessage
        {
            void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageExpression)
            {
                var sagaMember = Reflect<TSagaEntity>.GetMemberInfo(sagaEntityProperty, true);
                var sagaProp = sagaMember as PropertyInfo;
                if (sagaProp == null)
                {
                    throw new InvalidOperationException($"Mapping expressions for saga members must point to properties. Change member {sagaMember.Name} on {typeof(TSagaEntity).Name} to a property.");
                }

                ValidateMapping(messageExpression, sagaProp);

                ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);
                var compiledMessageExpression = messageExpression.Compile();
                var messageFunc = new Func<object, object>(o => compiledMessageExpression((TMessage) o));

                Mappings.Add(new SagaToMessageMap
                {
                    MessageProp = messageFunc,
                    SagaPropName = sagaProp.Name,
                    SagaPropType = sagaProp.PropertyType,
                    MessageType = typeof(TMessage)
                });
            }

            static void ValidateMapping<TMessage>(Expression<Func<TMessage, object>> messageExpression, PropertyInfo sagaProp)
            {
                var memberExpr = messageExpression.Body as MemberExpression;

                if (messageExpression.Body.NodeType == ExpressionType.Convert)
                {
                    memberExpr = ((UnaryExpression) messageExpression.Body).Operand as MemberExpression;
                }

                if (memberExpr == null)
                {
                    return;
                }

                var propertyInfo = memberExpr.Member as PropertyInfo;

                const string message = "When mapping a message to a saga, the member type on the message and the saga property must match. {0}.{1} is of type {2} and {3}.{4} is of type {5}.";

                if (propertyInfo != null)
                {
                    if (propertyInfo.PropertyType != sagaProp.PropertyType)
                    {
                        throw new InvalidOperationException(string.Format(message,
                            propertyInfo.DeclaringType.Name, propertyInfo.Name, propertyInfo.PropertyType,
                            sagaProp.DeclaringType.Name, sagaProp.Name, sagaProp.PropertyType));
                    }

                    return;
                }

                var fieldInfo = memberExpr.Member as FieldInfo;

                if (fieldInfo != null)
                {
                    if (fieldInfo.FieldType != sagaProp.PropertyType)
                    {
                        throw new InvalidOperationException(string.Format(message,
                            fieldInfo.DeclaringType.Name, fieldInfo.Name, fieldInfo.FieldType,
                            sagaProp.DeclaringType.Name, sagaProp.Name, sagaProp.PropertyType));
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
                        $@"Only public properties are supported for mapping Sagas. The lambda expression provided '{expression.Body}' is not mapping to a Property.");
                }
            }

            public List<SagaToMessageMap> Mappings = new List<SagaToMessageMap>();
        }

        /// <summary>
        /// Details about a saga data property used to correlate messages hitting the saga.
        /// </summary>
        public class CorrelationPropertyMetadata
        {
            /// <summary>
            /// Creates a new instance of <see cref="CorrelationPropertyMetadata" />.
            /// </summary>
            /// <param name="name">The name of the correlation property.</param>
            /// <param name="type">The type of the correlation property.</param>
            public CorrelationPropertyMetadata(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            /// <summary>
            /// The name of the correlation property.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// The type of the correlation property.
            /// </summary>
            public Type Type { get; }
        }
    }
}