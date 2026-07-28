#nullable enable

namespace NServiceBus;

static class ActivityDisplayNames
{
    public const string ProcessMessage = "process message";
    public const string PublishEvent = "publish event";
    public const string SubscribeEvent = "subscribe event";
    public const string UnsubscribeEvent = "unsubscribe event";
    public const string SendMessage = "send message";
    public const string ReplyMessage = "reply";

    // Operation-only prefixes used when UseMessageDestinationInSpanNames is enabled
    internal const string ProcessOperation = "process";
    internal const string PublishOperation = "publish";
    internal const string SendOperation = "send";
}