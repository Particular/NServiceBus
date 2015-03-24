namespace NServiceBus.Sagas
{
    using System;

    class SagaToMessageMap
    {
        public Func<object, object> MessageProp;
        public string SagaPropName;
        public Type MessageType;
        public Type CustomFinderType;

        public bool IsCustomFinderMap
        {
            get { return CustomFinderType != null; }
        }
    }
}