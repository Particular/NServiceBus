namespace NServiceBus.Core.Tests.Routing
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class ApplyReplyToAddressBehaviorTests
    {
        [Test]
        public async Task Should_set_the_reply_to_header_to_configured_address()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyAddress");
            var context = new OutgoingContext(new ContextBag());

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual("MyAddress", context.Get<DispatchMessageToTransportConnector.State>().Headers[Headers.ReplyToAddress]);
        }
    }
}