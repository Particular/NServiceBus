namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class MessageConventionException : Exception
    {
        public MessageConventionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MessageConventionException(SerializationInfo info, StreamingContext context)
        {
        }
    }
}