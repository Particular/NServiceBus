namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class SingleInstanceRoundRobinDistributionStrategyTests
    {
        [Test]
        public void ShouldRoundRobinOverAllProvidedInstances()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var endpointName = "endpointA";
            var instances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"2")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"3"))
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[2]));
        }

        [Test]
        public void ShouldRestartAtFirstInstance()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var endpointName = "endpointA";
            var instances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"2")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"3"))
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));

            Assert.That(result.Last(), Is.EqualTo(result.First()));
        }

        [Test]
        public void WhenNewInstancesAdded_ShouldIncludeAllInstancesInDistribution()
        {
            var endpointName = "endpointA";
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var instances = new List<UnicastRoutingTarget>
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"2")),
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));
            instances.Add(UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName + "3"))); // add new instance
            result.AddRange(strategy.SelectDestination(instances));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[2]));
        }

        [Test]
        public void WhenInstancesRemoved_ShouldOnlyDistributeAcrossRemainingInstances()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var endpointName = "endpointA";
            var instances = new List<UnicastRoutingTarget>
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"2")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, endpointName+"3"))
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));
            instances.RemoveAt(2); // remove last instance.
            result.AddRange(strategy.SelectDestination(instances));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(2).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
        }
    }
}