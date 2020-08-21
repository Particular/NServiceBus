namespace NServiceBus.Core.Tests.Performance.MessageDurability
{
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using NUnit.Framework;
    using Testing;

    public class DetermineMessageDurabilityBehaviorTests
    {
        [Test]
        public async Task When_message_is_non_durable_should_add_non_durable_constraint()
        {
            var behavior = new DetermineMessageDurabilityBehavior(t => true);
            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, _ => Task.CompletedTask);

            Assert.IsTrue(context.Extensions.TryGetDeliveryConstraint(out NonDurableDelivery nonDurableDeliveryConstraint));
            Assert.IsNotNull(nonDurableDeliveryConstraint);
        }

        [Test]
        public async Task When_message_is_non_durable_should_set_non_durable_header()
        {
            var behavior = new DetermineMessageDurabilityBehavior(t => true);
            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, _ => Task.CompletedTask);

            Assert.IsTrue(bool.Parse(context.Headers[Headers.NonDurableMessage]));
        }

        [Test]
        public async Task When_message_is_durable_should_not_add_non_durable_constraint()
        {
            var behavior = new DetermineMessageDurabilityBehavior(t => false);
            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, _ => Task.CompletedTask);

            Assert.IsFalse(context.Extensions.TryGetDeliveryConstraint(out NonDurableDelivery nonDurableDeliveryConstraint));
            Assert.IsNull(nonDurableDeliveryConstraint);
        }

        [Test]
        public async Task When_message_is_durable_should_not_set_non_durable_header()
        {
            var behavior = new DetermineMessageDurabilityBehavior(t => false);
            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, _ => Task.CompletedTask);

            Assert.IsFalse(context.Headers.ContainsKey(Headers.NonDurableMessage));
        }
    }
}