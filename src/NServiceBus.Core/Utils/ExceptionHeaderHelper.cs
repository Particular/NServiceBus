namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;

    static class ExceptionHeaderHelper
    {
        public static void SetExceptionHeaders(Dictionary<string, string> headers, ExceptionInfo exceptionInfo)
        {
            SetExceptionHeaders(headers, exceptionInfo, useLegacyStackTrace);
        }

        public static void SetExceptionHeaders(Dictionary<string, string> headers, ExceptionInfo exceptionInfo, bool legacyStacktrace)
        {
            headers["NServiceBus.ExceptionInfo.ExceptionType"] = exceptionInfo.TypeFullName;

            if (exceptionInfo.InnerExceptionTypeFullName != null)
            {
                headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = exceptionInfo.InnerExceptionTypeFullName;
            }

            headers["NServiceBus.ExceptionInfo.HelpLink"] = exceptionInfo.HelpLink;
            headers["NServiceBus.ExceptionInfo.Message"] = exceptionInfo.Message.Truncate(16384);
            headers["NServiceBus.ExceptionInfo.Source"] = exceptionInfo.Source;

            if (legacyStacktrace)
            {
                headers["NServiceBus.ExceptionInfo.StackTrace"] = exceptionInfo.LegacyStackTrace;
            }
            else
            {
                headers["NServiceBus.ExceptionInfo.StackTrace"] = exceptionInfo.StackTrace;
            }
            headers["NServiceBus.TimeOfFailure"] = exceptionInfo.TimeOfFailure;

            if (exceptionInfo.Data == null)
            {
                return;
            }

            foreach (var entry in exceptionInfo.Data)
            {
                headers["NServiceBus.ExceptionInfo.Data." + entry.Key] = entry.Value;
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