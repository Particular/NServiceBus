namespace MyMessages.RequestResponse
{
    using NServiceBus;

    public class InventoryResponse : IMessage
    {
        public int OrderNumber { get; set; }
        public string[] VideoIds { get; set; }
    }
}