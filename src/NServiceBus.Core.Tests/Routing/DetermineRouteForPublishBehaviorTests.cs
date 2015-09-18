namespace NServiceBus.Core.Tests.Routing
{
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class DetermineRouteForPublishBehaviorTests
    {
        [Test]
        public async Task Should_use_to_all_subscribers_strategy()
        {
            var behavior = new DetermineRouteForPublishBehavior();

            var context = new OutgoingPublishContext(new OutgoingLogicalMessage(new MyEvent()), new PublishOptions(), new RootContext(null));

            await behavior.Invoke(context, () => Task.FromResult(0));

            var routingStrategy = (ToAllSubscribers)context.Get<RoutingStrategy>();

            Assert.AreEqual(typeof(MyEvent), routingStrategy.EventType);
        }

        class MyEvent
        { }
    }
}