namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class ApplyReplyToAddressBehaviorTests
    {
        [Test]
        public async Task Should_set_the_reply_to_header_to_configured_address()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyAddress");
            var context = new OutgoingLogicalMessageContext(
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>(), 
                new OutgoingLogicalMessage(new MyMessage()), 
                new RoutingStrategy[] {},  
                new RootContext(null));

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual("MyAddress", context.Headers[Headers.ReplyToAddress]);
        }

        class MyMessage
        {
        }
    }
}