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
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA");

            var instances = new[]
            {
                "1",
                "2",
                "3"
            };

            var result = new List<string>();
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
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA");

            var instances = new[]
            {
                "1",
                "2",
                "3"
            };

            var result = new List<string>();
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));

            Assert.That(result.Last(), Is.EqualTo(result.First()));
        }

        [Test]
        public void WhenNewInstancesAdded_ShouldIncludeAllInstancesInDistribution()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA");

            var instances = new []
            {
                "1",
                "2",
            };

            var result = new List<string>();
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            instances = instances.Concat(new [] { "3" }).ToArray(); // add new instance
            result.Add(strategy.SelectReceiver(instances));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[2]));
        }

        [Test]
        public void WhenInstancesRemoved_ShouldOnlyDistributeAcrossRemainingInstances()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA");

            var instances = new []
            {
                "1",
                "2",
                "3"
            };

            var result = new List<string>();
            result.Add(strategy.SelectReceiver(instances));
            result.Add(strategy.SelectReceiver(instances));
            instances = instances.Take(2).ToArray(); // remove last instance.
            result.Add(strategy.SelectReceiver(instances));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(2).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
        }
    }
}