namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;

    static class ExceptionHeaderHelper
    {
        public static void SetExceptionHeaders(this TransportMessage message,Exception e, Address failedQueue, string reason = null)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                message.Headers["NServiceBus.ExceptionInfo.Reason"] = reason;
            }
            message.Headers["NServiceBus.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

            if (e.InnerException != null)
            {
                message.Headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            }

            message.Headers["NServiceBus.ExceptionInfo.HelpLink"] = e.HelpLink;
            message.Headers["NServiceBus.ExceptionInfo.Message"] = e.GetMessage();
            message.Headers["NServiceBus.ExceptionInfo.Source"] = e.Source;
            message.Headers["NServiceBus.ExceptionInfo.StackTrace"] = e.StackTrace;
            message.Headers[FaultsHeaderKeys.FailedQ] = failedQueue.ToString();
            message.Headers["NServiceBus.TimeOfFailure"] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
        }
    }
}
