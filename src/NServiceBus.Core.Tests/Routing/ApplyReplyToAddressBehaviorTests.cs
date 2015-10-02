namespace NServiceBus.Core.Tests.Routing
{
    using System.Threading.Tasks;
    using Extensibility;
    using OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class ApplyReplyToAddressBehaviorTests
    {
        [Test]
        public async Task Should_set_the_reply_to_header_to_configured_address()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyAddress");
            var context = new OutgoingLogicalMessageContext(new OutgoingLogicalMessage(new MyMessage()),  new ContextBag());

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual("MyAddress", context.Get<OutgoingPhysicalToRoutingConnector.State>().Headers[Headers.ReplyToAddress]);
        }

        class MyMessage
        {
        }
    }
}