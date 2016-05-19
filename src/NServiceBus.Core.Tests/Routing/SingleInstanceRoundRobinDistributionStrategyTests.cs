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

            var endpointName = new EndpointName("endpointA");
            var instances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "2")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "3"))
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

            var endpointName = new EndpointName("endpointA");
            var instances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "2")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "3"))
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(strategy.SelectDestination(instances));
            Enumerable.Range(1, 2).Select(_ => strategy.SelectDestination(instances)).ToList();
            result.AddRange(strategy.SelectDestination(instances));
            Enumerable.Range(1, 2).Select(_ => strategy.SelectDestination(instances)).ToList();
            result.AddRange(strategy.SelectDestination(instances));

            Assert.That(result, Has.All.SameAs(result.First()));
        }

        [Test]
        public void ShouldScopeDistributionToEndpointName()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var endpointA = new EndpointName("endpointA");
            var endpointAInstances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointA, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointA, "2")),
            };

            var endpointB = new EndpointName("endpointB");
            var endpointBInstances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointB, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointB, "2")),
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(strategy.SelectDestination(endpointAInstances));
            result.AddRange(strategy.SelectDestination(endpointBInstances));
            result.AddRange(strategy.SelectDestination(endpointAInstances));
            result.AddRange(strategy.SelectDestination(endpointBInstances));

            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[1]));
        }

        [Test]
        public void WhenNewInstancesAdded_ShouldIncludeAllInstancesInDistribution()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var endpointName = new EndpointName("endpointA");
            var instances = new List<UnicastRoutingTarget>
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "2")),
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(strategy.SelectDestination(instances));
            result.AddRange(strategy.SelectDestination(instances));
            instances.Add(UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "3"))); // add new instance
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

            var endpointName = new EndpointName("endpointA");
            var instances = new List<UnicastRoutingTarget>
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "2")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointName, "3"))
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