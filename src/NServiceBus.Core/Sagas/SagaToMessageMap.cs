namespace NServiceBus.Saga
{
    using System;

    class SagaToMessageMap
    {
        public Func<object, object> MessageProp;
        public string SagaPropName;
        public Type MessageType;
        public Type CustomFinderType;

        public bool HasCustomFinderMap
        {
            get { return CustomFinderType != null; }
        }
    }
}