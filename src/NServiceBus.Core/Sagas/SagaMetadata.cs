namespace NServiceBus.Saga
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Contains metadata for known sagas
    /// </summary>
    public class SagaMetadata
    {
        readonly Dictionary<string, SagaMessage> associatedMessages;
        readonly List<CorrelationProperty> correlationProperties;
        readonly string entityName;
        readonly string name;
        readonly Type sagaEntityType;
        readonly Dictionary<string, SagaFinderDefinition> sagaFinders;
        readonly Type sagaType;

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
            this.name = name;
            this.entityName = entityName;
            this.sagaEntityType = sagaEntityType;
            this.sagaType = sagaType;
            this.correlationProperties = correlationProperties;

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
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        ///     The name of the saga data entity.
        /// </summary>
        public string EntityName
        {
            get { return entityName; }
        }

        /// <summary>
        ///     The type of the related saga entity.
        /// </summary>
        public Type SagaEntityType
        {
            get { return sagaEntityType; }
        }

        /// <summary>
        ///     The type for this saga.
        /// </summary>
        public Type SagaType
        {
            get { return sagaType; }
        }

        /// <summary>
        ///     Properties this saga is correlated on.
        /// </summary>
        public List<CorrelationProperty> CorrelationProperties
        {
            get { return correlationProperties; }
        }

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
        /// <param name="messageType"></param>
        /// <param name="finderDefinition">The finder if present</param>
        /// <returns>True if finder exists</returns>
        public bool TryGetFinder(string messageType, out SagaFinderDefinition finderDefinition)
        {
            return sagaFinders.TryGetValue(messageType, out finderDefinition);
        }
    }
}