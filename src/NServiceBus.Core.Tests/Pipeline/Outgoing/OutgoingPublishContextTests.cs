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

            var testee = new OutgoingPublishContext(message, "message-id", options.OutgoingHeaders, options.Context, new RootContext(null, null, null));
            testee.Extensions.Set("someKey", "updatedValue");
            testee.Extensions.Set("anotherKey", "anotherValue");

            string value;
            string anotherValue;
            options.Context.TryGet("someKey", out value);
            Assert.AreEqual("someValue", value);
            Assert.IsFalse(options.Context.TryGet("anotherKey", out anotherValue));
            string updatedValue;
            string anotherValue2;
            testee.Extensions.TryGet("someKey", out updatedValue);
            testee.Extensions.TryGet("anotherKey", out anotherValue2);
            Assert.AreEqual("updatedValue", updatedValue);
            Assert.AreEqual("anotherValue", anotherValue2);
        }

        [Test]
        public void ShouldNotMergeOptionsToParentContext()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var options = new PublishOptions();
            options.Context.Set("someKey", "someValue");

            var parentContext = new RootContext(null,null,null);

            new OutgoingPublishContext(message, "message-id", options.OutgoingHeaders, options.Context, parentContext);

            string parentContextValue;
            var valueFound = parentContext.TryGet("someKey", out parentContextValue);

            Assert.IsFalse(valueFound);
        }
    }
}