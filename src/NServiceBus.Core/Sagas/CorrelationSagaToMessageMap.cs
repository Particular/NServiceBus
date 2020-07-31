namespace NServiceBus
{
    using System;

    abstract class CorrelationSagaToMessageMap : SagaToMessageMap
    {
        public string SagaPropName;
        public Type SagaPropType;
    }
}