namespace NServiceBus.AcceptanceTesting;

public class SubscriptionEvent
{
    /// <summary>
    /// The name of the subscriber endpoint.
    /// </summary>
    public required string SubscriberEndpoint { get; init; }

    /// <summary>
    /// The address of the subscriber.
    /// </summary>
    public string? SubscriberReturnAddress { get; init; }

    /// <summary>
    /// The type of message the client subscribed to.
    /// </summary>
    public required string MessageType { get; init; }
}