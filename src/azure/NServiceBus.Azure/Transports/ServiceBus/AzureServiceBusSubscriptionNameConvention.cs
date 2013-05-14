namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;

    public static class AzureServiceBusSubscriptionNameConvention
    {
        public static Func<Type, string> Create =  eventType => Configure.EndpointName + "." + eventType.Name;
    }
}