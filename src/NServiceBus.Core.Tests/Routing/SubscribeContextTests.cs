namespace NServiceBus.Core.Tests.Routing
{
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class SubscribeContextTests
    {
        [Test]
        public void ShouldShallowCloneContextBag()
        {
            var context = new SubscribeOptions();
            context.GetExtensions().Set("someKey", "someValue");

            var testee = new SubscribeContext(new FakeRootContext(), typeof(object), context);
            testee.Extensions.Set("someKey", "updatedValue");
            testee.Extensions.Set("anotherKey", "anotherValue");
            context.GetExtensions().TryGet("someKey", out string value);
            Assert.AreEqual("someValue", value);
            Assert.IsFalse(context.GetExtensions().TryGet("anotherKey", out string _));
            testee.Extensions.TryGet("someKey", out string updatedValue);
            testee.Extensions.TryGet("anotherKey", out string anotherValue2);
            Assert.AreEqual("updatedValue", updatedValue);
            Assert.AreEqual("anotherValue", anotherValue2);
        }

        [Test]
        public void ShouldNotMergeOptionsToParentContext()
        {
            var context = new SubscribeOptions();
            context.GetExtensions().Set("someKey", "someValue");

            var parentContext = new FakeRootContext();

            new SubscribeContext(parentContext, typeof(object), context);

            var valueFound = parentContext.TryGet("someKey", out string _);

            Assert.IsFalse(valueFound);
        }
    }
}