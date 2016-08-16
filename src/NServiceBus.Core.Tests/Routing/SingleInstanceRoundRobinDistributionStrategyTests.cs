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
                new EndpointInstance(endpointName, "1"),
                new EndpointInstance(endpointName, "2"),
                new EndpointInstance(endpointName, "3")
            };

            var result = new List<EndpointInstance>();
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));

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
                new EndpointInstance(endpointName, "1"),
                new EndpointInstance(endpointName, "2"),
                new EndpointInstance(endpointName, "3")
            };

            var result = new List<EndpointInstance>();
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));

            Assert.That(result.Last(), Is.EqualTo(result.First()));
        }

        [Test]
        public void WhenNewInstancesAdded_ShouldIncludeAllInstancesInDistribution()
        {
            var endpointName = "endpointA";
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var instances = new []
            {
                new EndpointInstance(endpointName, "1"),
                new EndpointInstance(endpointName, "2"),
            };

            var result = new List<EndpointInstance>();
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            instances = instances.Concat(new [] { new EndpointInstance(endpointName, "3")}).ToArray(); // add new instance
            result.Add(strategy.SelectReceiver(instances));

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
            var instances = new []
            {
                new EndpointInstance(endpointName, "1"),
                new EndpointInstance(endpointName, "2"),
                new EndpointInstance(endpointName, "3")
            };

            var result = new List<EndpointInstance>();
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            instances = instances.Take(2).ToArray(); // remove last instance.
            result.Add(strategy.SelectReceiver(instances));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(2).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
        }

        [Test]
        public void ShouldSeperateSubscriberFromInstanceDistribution()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy();

            var instances = new[]
            {
                new EndpointInstance("A", "instance1"),
                new EndpointInstance("A", "instance2"),
            };

            var subscribers = new[]
            {
                "subscriber1",
                "subscriber2",
            };

            var instance1 = strategy.SelectReceiver(instances);
            var subscriber1 = strategy.SelectSubscriber(subscribers);
            var instance2 = strategy.SelectReceiver(instances);
            var subscriber2 = strategy.SelectSubscriber(subscribers);

            Assert.That(instance1.Discriminator, Is.EqualTo("instance1"));
            Assert.That(instance2.Discriminator, Is.EqualTo("instance2"));
            Assert.That(subscriber1, Is.EqualTo("subscriber1"));
            Assert.That(subscriber2, Is.EqualTo("subscriber2"));
        }
    }
}