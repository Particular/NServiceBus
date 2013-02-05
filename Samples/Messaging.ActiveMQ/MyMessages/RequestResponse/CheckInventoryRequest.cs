namespace MyMessages.RequestResponse
{
    using NServiceBus;

    public class CheckInventoryRequest : IMessage
    {
        public int OrderNumber { get; set; }
        public string[] VideoIds { get; set; }
    }
}