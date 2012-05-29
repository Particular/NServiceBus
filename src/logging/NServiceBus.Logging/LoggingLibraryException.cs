using System;

namespace NServiceBus.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public class LoggingLibraryException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public LoggingLibraryException(string message)
            : base(message)
        {
        }
    }
}