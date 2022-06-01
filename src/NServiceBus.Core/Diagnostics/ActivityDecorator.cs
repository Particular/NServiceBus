namespace NServiceBus.Diagnostics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Pipeline;

    class ActivityDecorator
    {
        public static void SetReplyTags(Activity activity, string replyToAddress, IOutgoingReplyContext context)
        {
            if (activity != null)
            {
                activity.SetTag("NServiceBus.MessageId", context.MessageId);
                activity.SetTag("NServiceBus.ReplyToAddress", replyToAddress);
            }
        }

        public static void InjectHeaders(Activity activity, Dictionary<string, string> headers)
        {
            if (activity != null)
            {
                headers.Add("traceparent", activity.Id);
                if (activity.TraceStateString != null)
                {
                    headers["tracestate"] = activity.TraceStateString;
                }
            }
        }

        public static void SetSendTags(Activity activity, IOutgoingSendContext context)
        {
            activity?.SetTag("NServiceBus.MessageId", context.MessageId);
        }
    }
}