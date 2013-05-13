using System;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreateSubscriptionClients
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        SubscriptionClient Create(Address address, Type type);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="topicPath"></param>
        /// <param name="subscriptionname"></param>
        /// <returns></returns>
        SubscriptionClient Create(Type eventType, string topicPath, string subscriptionname);
    }
}