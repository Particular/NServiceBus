namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    class WrappedException : Exception
    {
        protected WrappedException(SerializationInfo info, StreamingContext context)
        {
        }
        public WrappedException(Exception exceptionThatIsWrapped)
            : base(String.Empty, exceptionThatIsWrapped)
        {
        }
    }
}