using NServiceBus;

namespace MyMessages
{
    public interface SubmitOrderResponse:IMessage
    {
        Order Order{ get; set; }
    }
}