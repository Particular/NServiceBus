using NServiceBus;

namespace Orders.Messages
{
    public class PlaceOrder : ICommand
    {
        public string OrderId { get; set; }
    }
}