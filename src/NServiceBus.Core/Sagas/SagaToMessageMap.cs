namespace NServiceBus
{
    using System;

    class SagaToMessageMap
    {
        public bool HasCustomFinderMap => CustomFinderType != null;
        public Type CustomFinderType;
        public Func<object, object> MessageProp;
        public Type MessageType;
        public string SagaPropName;
        public Type SagaPropType;
    }
}