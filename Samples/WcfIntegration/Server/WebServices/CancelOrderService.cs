using Messages;
using NServiceBus;

namespace Server.WebServices
{
    public class CancelOrderService : WcfService<CancelOrder, ErrorCodes>
    {
    }
}