namespace NServiceBus;

using Sagas;
using System;

abstract class SagaToMessageMap
{
    public Type MessageType { get; set; }

    public abstract SagaFinderDefinition CreateSagaFinderDefinition(Type sagaEntityType);
}