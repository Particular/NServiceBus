namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class OutgoingPublishContextTests
    {
        [Test]
        public void ShouldShallowCloneContext()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var options = new PublishOptions();
            options.Context.Set("someKey", "someValue");

            var testee = new OutgoingPublishContext(message, "message-id", options.OutgoingHeaders, options.Context, new RootContext(null, null, null, null));
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
            var options = new PublishOptions();
            options.Context.Set("someKey", "someValue");

            var parentContext = new RootContext(null, null, null, null);

            new OutgoingPublishContext(message, "message-id", options.OutgoingHeaders, options.Context, parentContext);

            var valueFound = parentContext.TryGet("someKey", out string _);

            Assert.IsFalse(valueFound);
        }
    }
}