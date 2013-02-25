namespace MyMessages.Events
{
    using NServiceBus;

    public interface OrderCancelled : IEvent
    {
        int OrderNumber { get; set; }
        string ClientId { get; set; }
    }
}