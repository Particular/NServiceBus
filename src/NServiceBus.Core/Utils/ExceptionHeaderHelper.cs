namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    static class ExceptionHeaderHelper
    {
        public static void SetExceptionHeaders(Dictionary<string, string> headers, Exception e)
        {
            headers["NServiceBus.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

            if (e.InnerException != null)
            {
                headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            }

            headers["NServiceBus.ExceptionInfo.HelpLink"] = e.HelpLink;
            headers["NServiceBus.ExceptionInfo.Message"] = e.GetMessage().Truncate(16384);
            headers["NServiceBus.ExceptionInfo.Source"] = e.Source;
            headers["NServiceBus.ExceptionInfo.StackTrace"] = e.ToString();
            headers["NServiceBus.TimeOfFailure"] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);

            if (e.Data == null)
            {
                return;
            }
#pragma warning disable DE0006
            foreach (DictionaryEntry entry in e.Data)
#pragma warning restore DE0006
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
    }
}