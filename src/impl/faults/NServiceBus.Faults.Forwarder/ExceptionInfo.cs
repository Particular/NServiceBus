using System;

namespace NServiceBus.Faults
{
    [Serializable]
    public class ExceptionInfo
    {
        public string HelpLink { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
    }

    public static class ExceptionInfoExtensions
    {
        public static ExceptionInfo GetInfo(this Exception e)
        {
            return new ExceptionInfo
                       {
                           HelpLink = e.HelpLink,
                           Message = e.Message,
                           Source = e.Source,
                           StackTrace = e.StackTrace
                       };
        }
    }
}
