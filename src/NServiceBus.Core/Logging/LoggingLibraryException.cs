namespace NServiceBus.Logging
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class LoggingLibraryException : Exception
    {
        public LoggingLibraryException(string message)
            : base(message)
        {
        }
        protected LoggingLibraryException(SerializationInfo info, StreamingContext context) { }
    }
}