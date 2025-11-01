namespace NServiceBus.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        associatedMessages = [];

        foreach (var sagaMessage in messages)
        {
            associatedMessages[sagaMessage.MessageTypeName] = sagaMessage;
        }

        sagaFinders = [];

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
    public string Name { get; }

    /// <summary>
    /// The name of the saga data entity.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// The type of the related saga entity.
    /// </summary>
    public Type SagaEntityType { get; }

    /// <summary>
    /// The type for this saga.
    /// </summary>
    public Type SagaType { get; }

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
        ArgumentNullException.ThrowIfNull(messageType);
        if (!associatedMessages.TryGetValue(messageType, out var sagaMessage))
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
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        return sagaFinders.TryGetValue(messageType, out finderDefinition);
    }

    /// <summary>
    /// Creates a <see cref="SagaMetadata" /> from a specific Saga type.
    /// </summary>
    /// <param name="sagaType">A type representing a Saga. Must be a non-generic type inheriting from <see cref="Saga" />.</param>
    /// <returns>An instance of <see cref="SagaMetadata" /> describing the Saga.</returns>
    public static SagaMetadata Create(Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);

        if (!IsSagaType(sagaType))
        {
            throw new Exception(sagaType.FullName + " is not a saga");
        }

        var genericArguments = GetBaseSagaType(sagaType).GetGenericArguments();
        if (genericArguments.Length != 1)
        {
            throw new Exception($"'{sagaType.Name}' saga type does not implement Saga<T>");
        }

        var saga = (Saga)RuntimeHelpers.GetUninitializedObject(sagaType);
        var associatedMessages = GetAssociatedMessages(sagaType)
            .ToList();

        var mapper = new SagaMapper(sagaType, associatedMessages);
        saga.ConfigureHowToFindSaga(mapper);

        var sagaEntityType = genericArguments.Single();

        var finders = new List<SagaFinderDefinition>();

        var propertyMappings = mapper.Mappings.OfType<CorrelationSagaToMessageMap>()
            .GroupBy(m => m.SagaPropName)
            .ToList();

        if (propertyMappings.Count > 1)
        {
            var messageTypes = string.Join(",", propertyMappings.SelectMany(g => g.Select(m => m.MessageType.FullName)).Distinct());
            throw new Exception($"Sagas can only have mappings that correlate on a single saga property. Use custom finders to correlate {messageTypes} to saga {sagaType.Name}");
        }

        CorrelationPropertyMetadata correlationProperty = null;

        if (propertyMappings.Count != 0)
        {
            var mapping = propertyMappings.Single().First();
            correlationProperty = new CorrelationPropertyMetadata(mapping.SagaPropName, mapping.SagaPropType);
        }

        foreach (var mapping in mapper.Mappings)
        {
            finders.Add(mapping.CreateSagaFinderDefinition(sagaEntityType));
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

    static List<SagaMessage> GetAssociatedMessages(Type sagaType)
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

    readonly Dictionary<string, SagaMessage> associatedMessages;
    readonly CorrelationPropertyMetadata correlationProperty;
    readonly Dictionary<string, SagaFinderDefinition> sagaFinders;

    // This list is also enforced at compile time in the SagaAnalyzer by diagnostic NSB0012,
    // but also needs to be enforced at runtime in case the user silences the diagnostic
    static readonly Type[] AllowedCorrelationPropertyTypes =
    {
        typeof(Guid), typeof(string), typeof(long), typeof(ulong), typeof(int), typeof(uint), typeof(short), typeof(ushort)
    };

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