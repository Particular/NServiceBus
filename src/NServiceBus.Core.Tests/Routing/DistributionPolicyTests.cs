namespace NServiceBus.Core.Tests.Routing
{
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class DistributionPolicyTests
    {
        [Test]
        public void When_no_strategy_configured_for_endpoint_should_use_round_robbin_strategy()
        {
            IDistributionPolicy policy = new DistributionPolicy(new SettingsHolder());

            var result = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Sends);

            Assert.That(result, Is.TypeOf<SingleInstanceRoundRobinDistributionStrategy>());
        }

        [Test]
        public void When_strategy_configured_for_endpoint_should_use_configured_strategy()
        {
            var p = new DistributionPolicy(new SettingsHolder());
            var configuredStrategy = new FakeDistributionStrategy();
            p.SetDistributionStrategy("SomeEndpoint", _ => configuredStrategy);

            IDistributionPolicy policy = p;
            var result = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Sends);

            Assert.That(result, Is.EqualTo(configuredStrategy));
        }

        [Test]
        public void When_requesting_a_strategy_should_always_return_the_same_instance()
        {
            var p = new DistributionPolicy(new SettingsHolder());
            p.SetDistributionStrategy("SomeEndpoint", _ => new FakeDistributionStrategy());

            IDistributionPolicy policy = p;
            var result1 = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Sends);
            var result2 = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Sends);

            Assert.AreSame(result1, result2);
        }

        [Test]
        public void When_requesting_a_strategy_for_sends_and_publishes_should_return_different_instances()
        {
            var p = new DistributionPolicy(new SettingsHolder());
            p.SetDistributionStrategy("SomeEndpoint", _ => new FakeDistributionStrategy());

            IDistributionPolicy policy = p;
            var result1 = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Sends);
            var result2 = policy.GetDistributionStrategy("SomeEndpoint", DistributionStrategyScope.Publishes);

            Assert.AreNotSame(result1, result2);
        }

        class FakeDistributionStrategy : DistributionStrategy
        {
            public override string SelectReceiver(string[] receiverAddresses)
            {
                return null;
            }
        }
    }
}