namespace NServiceBus.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// Contains metadata for known sagas.
/// </summary>
public partial class SagaMetadata
{
    SagaMetadata(Type sagaType, Type sagaEntityType, IReadOnlyCollection<SagaMessage> messages, SagaMapping mapping)
    {
        correlationProperty = mapping.CorrelationProperty;
        Name = sagaType.FullName;
        EntityName = sagaEntityType.FullName;
        SagaEntityType = sagaEntityType;
        SagaType = sagaType;

        associatedMessages = [];

        foreach (var sagaMessage in messages)
        {
            associatedMessages[sagaMessage.MessageTypeName] = sagaMessage;
        }

        sagaFinders = [];

        foreach (var finder in mapping.Finders)
        {
            sagaFinders[finder.MessageType.FullName!] = finder;
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

    internal static bool IsSagaType(Type t) => typeof(Saga).IsAssignableFrom(t) && t != typeof(Saga) && !t.IsGenericType && !t.IsAbstract;

    /// <summary>
    /// True if the specified message type is allowed to start the saga.
    /// </summary>
    public bool IsMessageAllowedToStartTheSaga(string messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return associatedMessages.TryGetValue(messageType, out var sagaMessage) && sagaMessage.IsAllowedToStartSaga;
    }

    /// <summary>
    /// Gets the configured finder for this message.
    /// </summary>
    /// <param name="messageType">The message <see cref="MemberInfo.Name" />.</param>
    /// <param name="finderDefinition">The finder if present.</param>
    /// <returns>True if a finder exists.</returns>
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

        var associatedMessages = GetAssociatedMessages(sagaType).ToList();

        var sagaEntityType = genericArguments.Single();

        return Create(sagaType, sagaEntityType, associatedMessages);
    }

    /// <summary>
    /// Creates a <see cref="SagaMetadata" /> from a specific Saga type.
    /// </summary>
    /// <typeparam name="TSaga">A type representing a Saga. Must be a non-generic type inheriting from <see cref="Saga" />.</typeparam>
    /// <typeparam name="TSagaData"></typeparam>
    /// <returns>An instance of <see cref="SagaMetadata" /> describing the Saga.</returns>
    public static SagaMetadata Create<TSaga, TSagaData>(IReadOnlyCollection<SagaMessage> associatedMessages)
        where TSaga : Saga<TSagaData>
        where TSagaData : class, IContainSagaData, new() =>
        Create(typeof(TSaga), typeof(TSagaData), associatedMessages);

    /// <summary>
    /// Creates a <see cref="SagaMetadata" /> from a specific Saga type.
    /// </summary>
    /// <typeparam name="TSagaType">A type representing a Saga. Must be a non-generic type inheriting from <see cref="Saga" />.</typeparam>
    /// <returns>An instance of <see cref="SagaMetadata" /> describing the Saga.</returns>
    public static SagaMetadata Create<TSagaType>() where TSagaType : Saga => Create(typeof(TSagaType));

    static SagaMetadata Create(Type sagaType, Type sagaEntityType, IReadOnlyCollection<SagaMessage> associatedMessages)
    {
        var saga = (Saga)RuntimeHelpers.GetUninitializedObject(sagaType);

        var mapper = new SagaMapper(sagaType, associatedMessages);

        saga.ConfigureHowToFindSaga(mapper);

        return new SagaMetadata(sagaType, sagaEntityType, associatedMessages, mapper.FinalizeMapping());
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

    /// <summary>
    /// Details about a saga data property used to correlate messages hitting the saga.
    /// </summary>
    /// <remarks>
    /// Creates a new instance of <see cref="CorrelationPropertyMetadata" />.
    /// </remarks>
    /// <param name="name">The name of the correlation property.</param>
    /// <param name="type">The type of the correlation property.</param>
    public class CorrelationPropertyMetadata(string name, Type type)
    {
        /// <summary>
        /// The name of the correlation property.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// The type of the correlation property.
        /// </summary>
        public Type Type { get; } = type;
    }
}