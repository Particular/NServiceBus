namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class OutgoingReplyContextTests
    {
        [Test]
        public void ShouldShallowCloneContext()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var options = new ReplyOptions();
            options.Context.Set("someKey", "someValue");

            var testee = new OutgoingReplyContext(message, "message-id", options.OutgoingHeaders, options, new FakeRootContext());
            testee.Extensions.Set("someKey", "updatedValue");
            testee.Extensions.Set("anotherKey", "anotherValue");

            options.Context.TryGet("someKey", out string value);
            Assert.AreEqual("someValue", value);
            Assert.IsFalse(options.Context.TryGet("anotherKey", out string _));
            testee.Extensions.TryGet("someKey", out string updatedValue);
            testee.Extensions.TryGet("anotherKey", out string anotherValue2);
            Assert.AreEqual("updatedValue", updatedValue);
            Assert.AreEqual("anotherValue", anotherValue2);
        }

        [Test]
        public void ShouldNotMergeOptionsToParentContext()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var options = new ReplyOptions();
            options.Context.Set("someKey", "someValue");

            var parentContext = new FakeRootContext();

            new OutgoingReplyContext(message, "message-id", options.OutgoingHeaders, options, parentContext);

            var valueFound = parentContext.TryGet("someKey", out string _);

            Assert.IsFalse(valueFound);
        }
    }
}