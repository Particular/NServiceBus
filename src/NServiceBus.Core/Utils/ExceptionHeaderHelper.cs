namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using NServiceBus.Faults;

    static class ExceptionHeaderHelper
    {
        static bool useLegacyStackTrace = String.Equals(ConfigurationManager.AppSettings["NServiceBus/Headers/UseLegacyExceptionStackTrace"], "true", StringComparison.OrdinalIgnoreCase);

        public static void SetExceptionHeaders(this TransportMessage message,Exception e, string failedQueue, string reason = null)
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
            headers["NServiceBus.ExceptionInfo.Message"] = e.GetMessage();
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
            if(e.Data == null)
// ReSharper disable once HeuristicUnreachableCode
                return;

            foreach (DictionaryEntry entry in e.Data)
            {
                if (entry.Value == null)
                    continue;
                headers["NServiceBus.ExceptionInfo.Data." + entry.Key] = entry.Value.ToString();
            }
        }
    }
}
