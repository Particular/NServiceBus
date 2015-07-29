namespace NServiceBus.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

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

        /// <summary>
        /// Scans through a collection of types and builds a <see cref="SagaMetadata" /> for each saga found.
        /// </summary>
        /// <param name="availableTypes">A collection of types to scan for sagas.</param>
        /// <returns>A collection of <see cref="SagaMetadata" /> for each saga found.</returns>
        public static IEnumerable<SagaMetadata> Create(IEnumerable<Type> availableTypes)
        {
            return TypeBasedSagaMetaModel.Create(availableTypes.ToList(), new Conventions());
        }

        /// <summary>
        /// Scans through a collection of types and builds a <see cref="SagaMetadata" /> for each saga found.
        /// </summary>
        /// <param name="availableTypes">A collection of types to scan for sagas.</param>
        /// <param name="conventions">Custom conventions to be used while scanning types.</param>
        /// <returns>A collection of <see cref="SagaMetadata" /> for each saga found.</returns>
        public static IEnumerable<SagaMetadata> Create(IEnumerable<Type> availableTypes, Conventions conventions)
        {
            return TypeBasedSagaMetaModel.Create(availableTypes.ToList(), conventions);
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
    }
}