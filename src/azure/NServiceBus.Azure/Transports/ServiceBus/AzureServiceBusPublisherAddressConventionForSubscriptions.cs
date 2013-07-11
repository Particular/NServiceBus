namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;

    public static class AzureServiceBusPublisherAddressConventionForSubscriptions
    {
        public static Func<Address, string> Create = AzureServiceBusPublisherAddressConvention.Create;
    }
}