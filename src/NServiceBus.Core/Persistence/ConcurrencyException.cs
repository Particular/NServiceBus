namespace NServiceBus.Persistence
{
    using System;

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
    }
}