using System;

namespace NServiceBus.Logging
{
    public class LoggingLibraryException : Exception
    {
        public LoggingLibraryException(string message)
            : base(message)
        {
        }
    }
}