using NServiceBus;

namespace MyMessages
{
    public interface OrderUpdatedEvent:IMessage
    {
        Order UpdatedOrder{ get; set; }
    }
}