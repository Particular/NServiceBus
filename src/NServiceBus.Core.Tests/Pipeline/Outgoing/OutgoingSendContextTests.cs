namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class OutgoingSendContextTests
    {
        [Test]
        public void ShouldShallowCloneContext()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var options = new SendOptions();
            options.Context.Set("someKey", "someValue");

            var testee = new OutgoingSendContext(message, "message-id", options.OutgoingHeaders, options.Context, new RootContext(null, null, null));
            testee.Extensions.Set("someKey", "updatedValue");
            testee.Extensions.Set("anotherKey", "anotherValue");

            string value;
            options.Context.TryGet("someKey", out value);
            Assert.AreEqual("someValue", value);
            Assert.IsFalse(options.Context.TryGet("anotherKey", out string _));
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
            var options = new SendOptions();
            options.Context.Set("someKey", "someValue");

            var parentContext = new RootContext(null, null, null);

            new OutgoingSendContext(message, "message-id", options.OutgoingHeaders, options.Context, parentContext);

            var valueFound = parentContext.TryGet("someKey", out string _);

            Assert.IsFalse(valueFound);
        }
    }
}