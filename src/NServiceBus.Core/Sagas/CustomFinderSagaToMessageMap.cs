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
}