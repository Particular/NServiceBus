namespace VideoStore.Messages.Events
{
    //NServiceBus messages can be defined using both classes and interfaces
    public interface OrderAccepted 
    {
        int OrderNumber { get; set; }
        string[] VideoIds { get; set; }
        string ClientId { get; set; }
    }
}