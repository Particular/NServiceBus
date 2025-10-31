namespace NServiceBus;

using Sagas;
using System;

class CustomFinderSagaToMessageMap : SagaToMessageMap
{
    public Type CustomFinderType;

    public override SagaFinderDefinition CreateSagaFinderDefinition(Type sagaEntityType)
    {
        var finderType = typeof(CustomFinderAdapter<,,>).MakeGenericType(CustomFinderType, sagaEntityType, MessageType);
        return new SagaFinderDefinition(finderType, MessageType, []);
    }

    protected override string SagaDoesNotHandleMappedMessage(Type sagaType)
    {
        var msgType = MessageType.FullName;
        return $"Custom saga finder {CustomFinderType.FullName} maps message type {msgType} for saga {sagaType.Name}, but the saga does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.";
    }
}