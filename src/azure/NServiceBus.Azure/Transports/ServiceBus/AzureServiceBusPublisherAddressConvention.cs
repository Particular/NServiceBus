namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;

    public static class AzureServiceBusPublisherAddressConvention
    {
        public static Func<Address, string> Create = address => address.Queue + ".events";
        
    }
}