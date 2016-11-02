namespace NServiceBus.Core.Tests.Routing
{
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class DistributionPolicyTests
    {
        [Test]
        public void When_no_strategy_configured_for_endpoint_should_use_round_robbin_strategy()
        {
            var policy = new DistributionPolicy();

            var result = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Send);

            Assert.That(result, Is.TypeOf<SingleInstanceRoundRobinDistributionStrategy>());
        }

        [Test]
        public void When_strategy_configured_for_endpoint_should_use_configured_strategy()
        {
            var policy = new DistributionPolicy();
            var configuredStrategy = new FakeDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Send);
            policy.SetDistributionStrategy(configuredStrategy);

            var result = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Send);

            Assert.That(result, Is.EqualTo(configuredStrategy));
        }

        [Test]
        public void When_multiple_strategies_configured_endpoint_should_use_last_configured_strategy()
        {
            var policy = new DistributionPolicy();
            var strategy1 = new FakeDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Send);
            var strategy2 = new FakeDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Send);
            policy.SetDistributionStrategy(strategy1);
            policy.SetDistributionStrategy(strategy2);

            var result = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Send);

            Assert.That(result, Is.EqualTo(strategy2));
        }

        class FakeDistributionStrategy : DistributionStrategy
        {
            public FakeDistributionStrategy(string endpoint, DistributionStrategyScope scope) : base(endpoint, scope)
            {
            }

            public override string SelectReceiver(string[] receiverAddresses)
            {
                return null;
            }
        }
    }
}