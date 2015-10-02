namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
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
            var behavior = new IndirectPublishRouterBehavior();

            var context = new OutgoingPublishContext(new OutgoingLogicalMessage(new MyEvent()), new PublishOptions(), new RootContext(null));

            IndirectAddressLabel addressLabel = null;
            await behavior.Invoke(context, _ =>
            {
                addressLabel = _.AddressLabels.Cast<IndirectAddressLabel>().Single();
                return Task.FromResult(0);
            });

            Assert.AreEqual(typeof(MyEvent), addressLabel.MessageType);
        }

        class MyEvent
        { }
    }
}