namespace NServiceBus
{
    using System;

    class SagaToMessageMap
    {
        public Func<object, object> MessageProp;
        public string SagaPropName;
        public Type SagaPropType;
        public Type MessageType;
        public Type CustomFinderType;
        public bool HasCustomFinderMap => CustomFinderType != null;
    }
}