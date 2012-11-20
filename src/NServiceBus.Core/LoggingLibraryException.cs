namespace NServiceBus
{
    using System;

    public class LoggingLibraryException : Exception
    {
        public LoggingLibraryException(string message) : base(message)
        {
        }
    }
}