namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains metadata for known sagas
    /// </summary>
    class SagaMetadata
    {
        public SagaMetadata(IEnumerable<SagaMessage> messages, IEnumerable<SagaFinderDefinition> finders)
        {
            CorrelationProperties = new List<CorrelationProperty>();

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
        /// Properties this saga is correlated on
        /// </summary>
        public List<CorrelationProperty> CorrelationProperties;

        /// <summary>
        /// The name of the saga
        /// </summary>
        public string Name;

        /// <summary>
        /// The name of the saga data entity
        /// </summary>
        public string EntityName;

        /// <summary>
        /// True if the specified message type is allowed to start the saga
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
        /// Returns the list of messages that is associated with this saga
        /// </summary>
        public IEnumerable<SagaMessage> AssociatedMessages
        {
            get { return associatedMessages.Values; }
        }

        /// <summary>
        /// Gets the list of finders for this saga
        /// </summary>
        public IEnumerable<SagaFinderDefinition> Finders
        {
            get { return sagaFinders.Values; }
        }

        /// <summary>
        /// The type of the related saga entity
        /// </summary>
        public Type SagaEntityType;

        /// <summary>
        /// The type for this saga
        /// </summary>
        public Type SagaType;


        /// <summary>
        /// Gets the configured finder for this message
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="finderDefinition">The finder if present</param>
        /// <returns>True if finder exists</returns>
        public bool TryGetFinder(string messageType,out SagaFinderDefinition finderDefinition)
        {
            return sagaFinders.TryGetValue(messageType,out finderDefinition);
        }

        Dictionary<string, SagaMessage> associatedMessages;
        Dictionary<string, SagaFinderDefinition> sagaFinders;
    }
}