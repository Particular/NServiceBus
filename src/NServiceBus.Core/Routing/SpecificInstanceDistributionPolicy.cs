namespace NServiceBus
{
    using System;
    using System.Linq;
    using Routing;

    class SpecificInstanceDistributionPolicy : IDistributionPolicy
    {
        readonly string specificInstance;

        public SpecificInstanceDistributionPolicy(string specificInstance)
        {
            this.specificInstance = specificInstance;
        }

        public DistributionStrategy GetDistributionStrategy(string endpointName)
        {
            return new SpecificInstanceDistributionStrategy(specificInstance);
        }

        class SpecificInstanceDistributionStrategy : DistributionStrategy
        {
            public SpecificInstanceDistributionStrategy(string specificInstance)
            {
                this.specificInstance = specificInstance;
            }

            public override EndpointInstance SelectDestination(EndpointInstance[] allInstances)
            {
                var target = allInstances.FirstOrDefault(t => t.Discriminator == specificInstance);
                if (target == null)
                {
                    throw new Exception($"Specified instance {specificInstance} has not been configured in the routing tables.");
                }
                return target;
            }

            string specificInstance;
        }
    }
}