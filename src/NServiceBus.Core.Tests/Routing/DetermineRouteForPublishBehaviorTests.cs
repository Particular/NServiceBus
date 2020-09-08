﻿namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;

    public class DetermineRouteForPublishBehaviorTests
    {
        [Test]
        public async Task Should_use_to_all_subscribers_strategy()
        {
            var behavior = new MulticastPublishConnector();

            var context = new TestableOutgoingPublishContext
            {
                Message = new OutgoingLogicalMessage(typeof(MyEvent), new MyEvent())
            };

            MulticastAddressTag addressTag = null;
            await behavior.Invoke(context, (ctx, ct) =>
            {
                addressTag = (MulticastAddressTag)ctx.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return Task.CompletedTask;
            }, CancellationToken.None);

            Assert.AreEqual(typeof(MyEvent), addressTag.MessageType);
        }

        class MyEvent
        { }
    }
}