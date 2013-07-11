namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;

    public static class AzureServiceBusSubscriptionNameConvention
    {
        public static Func<Type, string> Create = eventType => eventType != null ? Configure.EndpointName + "." + eventType.Name : Configure.EndpointName;
    }
}