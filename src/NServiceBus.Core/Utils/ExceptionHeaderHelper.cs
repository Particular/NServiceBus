namespace NServiceBus
{
    using System.Collections.Generic;

    static class ExceptionHeaderHelper
    {
        public static void SetExceptionHeaders(Dictionary<string, string> headers, ExceptionInfo exceptionInfo)
        {
            headers["NServiceBus.ExceptionInfo.ExceptionType"] = exceptionInfo.TypeFullName;

            if (exceptionInfo.InnerExceptionTypeFullName != null)
            {
                headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = exceptionInfo.InnerExceptionTypeFullName;
            }

            headers["NServiceBus.ExceptionInfo.HelpLink"] = exceptionInfo.HelpLink;
            headers["NServiceBus.ExceptionInfo.Message"] = exceptionInfo.Message;
            headers["NServiceBus.ExceptionInfo.Source"] = exceptionInfo.Source;
            headers["NServiceBus.ExceptionInfo.StackTrace"] = exceptionInfo.StackTrace;
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
    }
}