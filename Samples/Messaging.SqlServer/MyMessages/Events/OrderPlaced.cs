namespace MyMessages.Events
{
    using NServiceBus;

    public interface OrderPlaced : IEvent
    {
        int OrderNumber { get; set; }
        string[] VideoIds { get; set; }
        string ClientId { get; set; }
    }
}