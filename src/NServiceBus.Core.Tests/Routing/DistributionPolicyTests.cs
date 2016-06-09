namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class DistributionPolicyTests
    {
        [Test]
        public void When_no_strategy_configured_for_endpoint_should_use_round_robbin_strategy()
        {
            var policy = new DistributionPolicy();

            var result = policy.GetDistributionStrategy("SomeEndpoint");

            Assert.That(result, Is.TypeOf<SingleInstanceRoundRobinDistributionStrategy>());
        }

        [Test]
        public void When_strategy_configured_for_endpoint_should_use_configured_strategy()
        {
            var policy = new DistributionPolicy();
            var configuredStrategy = new FakeDistributionStrategy();
            policy.SetDistributionStrategy("SomeEndpoint", configuredStrategy);

            var result = policy.GetDistributionStrategy("SomeEndpoint");

            Assert.That(result, Is.EqualTo(configuredStrategy));
        }

        [Test]
        public void When_multiple_strategies_configured_endpoint_should_use_last_configured_strategy()
        {
            var policy = new DistributionPolicy();
            var strategy = new FakeDistributionStrategy();
            policy.SetDistributionStrategy("SomeEndpoint", new FakeDistributionStrategy());
            policy.SetDistributionStrategy("SomeEndpoint", new FakeDistributionStrategy());
            policy.SetDistributionStrategy("SomeEndpoint", strategy);

            var result = policy.GetDistributionStrategy("SomeEndpoint");

            Assert.That(result, Is.EqualTo(strategy));
        }

        class FakeDistributionStrategy : DistributionStrategy
        {
            public override IEnumerable<UnicastRoutingTarget> SelectDestination(IList<UnicastRoutingTarget> allInstances)
            {
                return null;
            }
        }
    }
}