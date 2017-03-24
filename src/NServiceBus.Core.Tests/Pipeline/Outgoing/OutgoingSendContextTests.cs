namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class OutgoingSendContextTests
    {
        [Test]
        public void ShouldShallowCloneHeaders()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var options = new SendOptions();
            options.SetHeader("someHeader", "someValue");

            var testee = new OutgoingSendContext(message, options, new RootContext(null, null, null));
            testee.Headers["someHeader"] = "updatedValue";
            testee.Headers["anotherHeader"] = "anotherValue";

            Assert.AreEqual("someValue", options.OutgoingHeaders["someHeader"]);
            Assert.IsFalse(options.OutgoingHeaders.ContainsKey("anotherHeader"));
            Assert.AreEqual("updatedValue", testee.Headers["someHeader"]);
            Assert.AreEqual("anotherValue", testee.Headers["anotherHeader"]);
        }

        [Test]
        public void ShouldShallowCloneContext()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var options = new SendOptions();
            options.Context.Set("someKey", "someValue");

            var testee = new OutgoingSendContext(message, options, new RootContext(null, null, null));
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
            var options = new SendOptions();
            options.Context.Set("someKey", "someValue");

            var parentContext = new RootContext(null, null, null);

            new OutgoingSendContext(message, options, parentContext);

            string parentContextValue;
            var valueFound = parentContext.TryGet("someKey", out parentContextValue);

            Assert.IsFalse(valueFound);
        }
    }
}