namespace NServiceBus
{
    using Sagas;
    using System;
    using System.Collections.Generic;

    class CustomFinderSagaToMessageMap : SagaToMessageMap
    {
        public Type CustomFinderType;

        public override SagaFinderDefinition CreateSagaFinderDefinition(Type sagaEntityType)
        {
            return new SagaFinderDefinition(
                typeof(CustomFinderAdapter<,>).MakeGenericType(sagaEntityType, MessageType),
                MessageType,
                new Dictionary<string, object>
                {
                    {"custom-finder-clr-type", CustomFinderType}
                });
        }

        protected override string SagaDoesNotHandleMappedMessage(Type sagaType)
        {
            var msgType = MessageType.FullName;
            return $"Custom saga finder {CustomFinderType.FullName} maps message type {msgType} for saga {sagaType.Name}, but the saga does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.";
        }
    }
}