namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
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

            var testee = new OutgoingSendContext(message, options, new RootContext(null));
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

            var testee = new OutgoingSendContext(message, options, new RootContext(null));
            testee.Set("someKey", "updatedValue");
            testee.Set("anotherKey", "anotherValue");

            string value;
            string anotherValue;
            options.Context.TryGet("someKey", out value);
            Assert.AreEqual("someValue", value);
            Assert.IsFalse(options.Context.TryGet("anotherKey", out anotherValue));
            string updatedValue;
            string anotherValue2;
            testee.TryGet("someKey", out updatedValue);
            testee.TryGet("anotherKey", out anotherValue2);
            Assert.AreEqual("updatedValue", updatedValue);
            Assert.AreEqual("anotherValue", anotherValue2);
        }
    }
}