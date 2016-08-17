namespace NServiceBus
{
    using System;
    using System.Linq;
    using Routing;

    class SpecificInstanceDistributionPolicy : IDistributionPolicy
    {
        public SpecificInstanceDistributionPolicy(string discriminator, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.discriminator = discriminator;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public DistributionStrategy GetDistributionStrategy(string endpointName, DistributionStrategyScope scope)
        {
            return new SpecificInstanceDistributionStrategy(
                new EndpointInstance(endpointName, discriminator),
                transportAddressTranslation, scope);
        }

        readonly Func<EndpointInstance, string> transportAddressTranslation;
        string discriminator;

        class SpecificInstanceDistributionStrategy : DistributionStrategy
        {
            public SpecificInstanceDistributionStrategy(EndpointInstance instance, Func<EndpointInstance, string> transportAddressTranslation, DistributionStrategyScope scope) : base(instance.Endpoint, scope)
            {
                this.instance = instance;
                this.transportAddressTranslation = transportAddressTranslation;
            }

            public override string SelectReceiver(string[] receiverAddresses)
            {
                var instanceAddress = transportAddressTranslation(instance);
                var target = receiverAddresses.FirstOrDefault(t => t == instanceAddress);
                if (target == null)
                {
                    throw new Exception($"Specified instance with discriminator {instance.Discriminator} has not been configured in the routing tables.");
                }
                return target;
            }

            readonly Func<EndpointInstance, string> transportAddressTranslation;

            EndpointInstance instance;
        }
    }
}