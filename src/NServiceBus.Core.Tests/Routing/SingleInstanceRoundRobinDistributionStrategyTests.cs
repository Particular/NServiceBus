namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class SingleInstanceRoundRobinDistributionStrategyTests
    {
        [Test]
        public async Task ShouldRoundRobinOverAllProvidedInstances()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA", DistributionStrategyScope.Send);

            var instances = new[]
            {
                "1",
                "2",
                "3"
            };

            var distributionContext = new DistributionContext(instances, null, null, null, null, null);
            var result = new List<string>
            {
                await strategy.SelectDestination(distributionContext),
                await strategy.SelectDestination(distributionContext),
                await strategy.SelectDestination(distributionContext)
            };

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[2]));
        }

        [Test]
        public async Task ShouldRestartAtFirstInstance()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA", DistributionStrategyScope.Send);

            var instances = new[]
            {
                "1",
                "2",
                "3"
            };

            var distributionContext = new DistributionContext(instances, null, null, null, null, null);
            var result = new List<string>
            {
                await strategy.SelectDestination(distributionContext),
                await strategy.SelectDestination(distributionContext),
                await strategy.SelectDestination(distributionContext),
                await strategy.SelectDestination(distributionContext)
            };

            Assert.That(result.Last(), Is.EqualTo(result.First()));
        }

        [Test]
        public async Task WhenNewInstancesAdded_ShouldIncludeAllInstancesInDistribution()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA", DistributionStrategyScope.Send);

            var instances = new[]
            {
                "1",
                "2",
            };

            var distributionContext = new DistributionContext(instances, null, null, null, null, null);
            var result = new List<string>
            {
                await strategy.SelectDestination(distributionContext),
                await strategy.SelectDestination(distributionContext)
            };
            instances = instances.Concat(new[] { "3" }).ToArray(); // add new instance
            distributionContext = new DistributionContext(instances, null, null, null, null, null);
            result.Add(await strategy.SelectDestination(distributionContext));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[2]));
        }

        [Test]
        public async Task WhenInstancesRemoved_ShouldOnlyDistributeAcrossRemainingInstances()
        {
            var strategy = new SingleInstanceRoundRobinDistributionStrategy("endpointA", DistributionStrategyScope.Send);

            var instances = new[]
            {
                "1",
                "2",
                "3"
            };

            var distributionContext = new DistributionContext(instances, null, null, null, null, null);
            var result = new List<string>
            {
                await strategy.SelectDestination(distributionContext),
                await strategy.SelectDestination(distributionContext)
            };
            instances = instances.Take(2).ToArray(); // remove last instance.
            distributionContext = new DistributionContext(instances, null, null, null, null, null);
            result.Add(await strategy.SelectDestination(distributionContext));

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Has.Exactly(2).EqualTo(instances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(instances[1]));
        }
    }
}