namespace NServiceBus.Core.Tests.Routing
{
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class ApplyReplyToAddressBehaviorTests
    {
        [Test]
        public void Should_set_the_reply_to_header_to_configured_address()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyAddress");
            var context = new OutgoingContext(new IncomingContext(null));

            behavior.Invoke(context, () => { });

            Assert.AreEqual("MyAddress", context.Get<DispatchMessageToTransportConnector.State>().Headers[Headers.ReplyToAddress]);
        }
    }
}