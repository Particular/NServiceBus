namespace NServiceBus
{
    using Sagas;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    abstract class SagaToMessageMap
    {
        public Type MessageType { get; set; }

        public abstract SagaFinderDefinition CreateSagaFinderDefinition(Type sagaEntityType);

        public void AssertSagaHandlesMappedMessage(Type sagaType, IEnumerable<SagaMessage> associatedMessages)
        {
            var associatedMessage = associatedMessages.FirstOrDefault(m => MessageType.IsAssignableFrom(m.MessageType));
            if (associatedMessage == null)
            {
                throw new Exception(SagaDoesNotHandleMappedMessage(sagaType));
            }
        }

        protected virtual string SagaDoesNotHandleMappedMessage(Type sagaType)
        {
            var msgType = MessageType.FullName;
            return $"Saga {sagaType.FullName} contains a mapping for {msgType} in the ConfigureHowToFindSaga method, but does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.";
        }
    }
}