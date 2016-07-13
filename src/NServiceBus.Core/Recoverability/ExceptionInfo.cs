namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Exception details for failing message.
    /// </summary>
    public class ExceptionInfo
    {
        /// <summary>
        /// Exception type full name.
        /// </summary>
        public string TypeFullName { get; }

        /// <summary>
        /// Inner exception type full name.
        /// </summary>
        public string InnerExceptionTypeFullName { get; }

        /// <summary>
        /// Exception <see cref="Exception.HelpLink"/>.
        /// </summary>
        public string HelpLink { get; }

        /// <summary>
        /// Exception <see cref="Exception.Message"/>.
        /// </summary>
        public string Message { get;}

        /// <summary>
        /// Exception <see cref="Exception.Source"/>.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Exception <see cref="Exception.ToString"/>.
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Time of failure in 'yyyy-MM-dd HH:mm:ss:ffffff Z' format.
        /// </summary>
        public string TimeOfFailure { get; }

        /// <summary>
        /// Exception <see cref="Exception.Data"/>.
        /// </summary>
        public Dictionary<string, string> Data { get; }


        /// <summary>
        /// Creates new instance of <see cref="ExceptionInfo"/>.
        /// </summary>
        public ExceptionInfo(string typeFullName, string innerExceptionTypeFullName, string helpLink, string message, string source, string stackTrace, string timeOfFailure, Dictionary<string, string> data)
        {
            TypeFullName = typeFullName;
            InnerExceptionTypeFullName = innerExceptionTypeFullName;
            HelpLink = helpLink;
            Message = message;
            Source = source;
            StackTrace = stackTrace;
            TimeOfFailure = timeOfFailure;
            Data = data;
        }

        /// <summary>
        /// Crates <see cref="ExceptionInfo"/> from and instance of <see cref="Exception"/>.
        /// </summary>
        public static ExceptionInfo FromException(Exception e)
        {
            Dictionary<string, string> data = null;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (e.Data != null)
            {
                data = new Dictionary<string, string>();

                foreach (DictionaryEntry entry in e.Data)
                {
                    if (entry.Value == null)
                    {
                        continue;
                    }

                    data.Add(entry.Value.ToString(), entry.Value.ToString());
                }
            }

            return new ExceptionInfo(
                e.GetType().FullName,
                e.InnerException?.GetType().FullName,
                e.HelpLink,
                e.Message,
                e.Source,
                e.ToString(),
                DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow),
                data
            );
        }
    }
}