namespace NServiceBus.Core.Tests.Routing
{
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class DetermineRouteForPublishBehaviorTests
    {
        [Test]
        public void Should_use_to_all_subscribers_strategy()
        {
            var behavior = new DetermineRouteForPublishBehavior();

            var context = new OutgoingPublishContext(new RootContext(null), new OutgoingLogicalMessage(new MyEvent()), new PublishOptions());

            behavior.Invoke(context, () => { });

            var routingStrategy = (ToAllSubscribers)context.Get<RoutingStrategy>();

            Assert.AreEqual(typeof(MyEvent), routingStrategy.EventType);
        }

        class MyEvent
        { }
    }
}