#nullable enable

namespace NServiceBus;

// Dispatched through delayed delivery; the timeout handler receives the payload stored with the saga, not this message.
sealed class SagaTimeoutTrigger
{
    public string TimeoutId { get; set; } = null!;
    public string TriggerId { get; set; } = null!;
    public string TimeoutMessageType { get; set; } = null!;
}
