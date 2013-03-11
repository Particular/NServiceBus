namespace MyMessages.Events
{
    using NServiceBus;

    //NServiceBus messages can be defined using both classes and interfaces
    public interface OrderAccepted : IEvent
    {
        int OrderNumber { get; set; }
        string[] VideoIds { get; set; }
        string ClientId { get; set; }
    }
}