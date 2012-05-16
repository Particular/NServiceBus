using System;

namespace NServiceBus
{
    public class LoggingLibraryException : Exception
    {
        public LoggingLibraryException(string message) : base(message)
        {
        }
    }
}