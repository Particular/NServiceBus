namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using Faults;
    using Transports;

    static class ExceptionHeaderHelper
    {
        public static void SetExceptionHeaders(this IncomingMessage message, Exception e, string failedQueue, string reason = null)
        {
            var headers = message.Headers;
            SetExceptionHeaders(headers, e, failedQueue, reason, useLegacyStackTrace);
        }

        public static void SetExceptionHeaders(this OutgoingMessage message, Exception e, string failedQueue, string reason = null)
        {
            var headers = message.Headers;
            SetExceptionHeaders(headers, e, failedQueue, reason, useLegacyStackTrace);
        }

        internal static void SetExceptionHeaders(Dictionary<string, string> headers, Exception e, string failedQueue, string reason, bool legacyStackTrace)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                headers["NServiceBus.ExceptionInfo.Reason"] = reason;
            }
            headers["NServiceBus.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

            if (e.InnerException != null)
            {
                headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            }

            headers["NServiceBus.ExceptionInfo.HelpLink"] = e.HelpLink;
            headers["NServiceBus.ExceptionInfo.Message"] = e.GetMessage().Truncate(16384);
            headers["NServiceBus.ExceptionInfo.Source"] = e.Source;
            if (legacyStackTrace)
            {
                headers["NServiceBus.ExceptionInfo.StackTrace"] = e.StackTrace;
            }
            else
            {
                headers["NServiceBus.ExceptionInfo.StackTrace"] = e.ToString();
            }
            headers[FaultsHeaderKeys.FailedQ] = failedQueue;
            headers["NServiceBus.TimeOfFailure"] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (e.Data == null)
            // ReSharper disable HeuristicUnreachableCode
            {
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            foreach (DictionaryEntry entry in e.Data)
            {
                if (entry.Value == null)
                {
                    continue;
                }
                headers["NServiceBus.ExceptionInfo.Data." + entry.Key] = entry.Value.ToString();
            }
        }

        static string Truncate(this string value, int maxLength) =>
            string.IsNullOrEmpty(value)
                ? value
                : (value.Length <= maxLength
                    ? value
                    : value.Substring(0, maxLength));

        static bool useLegacyStackTrace = string.Equals(ConfigurationManager.AppSettings["NServiceBus/Headers/UseLegacyExceptionStackTrace"], "true", StringComparison.OrdinalIgnoreCase);
    }
}