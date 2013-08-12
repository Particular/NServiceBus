namespace NServiceBus.Persistence
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ConcurrencyException(string message) : base(message)
        {
        }

        public ConcurrencyException()
        {
        }
        protected ConcurrencyException(SerializationInfo info, StreamingContext context) { }
    }
}