using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Windows.ServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreateQueueClients
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        QueueClient Create(Address address);
    }
}
