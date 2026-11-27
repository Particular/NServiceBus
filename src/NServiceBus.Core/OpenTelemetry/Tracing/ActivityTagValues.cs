#nullable enable

namespace NServiceBus;

// Values written to the ActivityTags.RecoverabilityAction tag.
static class ActivityTagValues
{
    public const string ImmediateRetry = "immediate_retry";
    public const string DelayedRetry = "delayed_retry";
    public const string MoveToError = "move_to_error";
    public const string Discard = "discard";
}
