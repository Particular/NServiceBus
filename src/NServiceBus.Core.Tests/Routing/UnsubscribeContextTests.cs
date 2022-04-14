namespace NServiceBus.Core.Tests.Routing
{
    using DeliveryConstraints;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class UnsubscribeContextTests
    {
        [Test]
        public void ShouldShallowCloneContext()
        {
            var context = new ContextBag();
            context.Set("someKey", "someValue");

            var testee = new UnsubscribeContext(new FakeRootContext(), typeof(object), context);
            testee.Extensions.Set("someKey", "updatedValue");
            testee.Extensions.Set("anotherKey", "anotherValue");
            context.TryGet("someKey", out string value);
            Assert.AreEqual("someValue", value);
            Assert.IsFalse(context.TryGet("anotherKey", out string _));
            testee.Extensions.TryGet("someKey", out string updatedValue);
            testee.Extensions.TryGet("anotherKey", out string anotherValue2);
            Assert.AreEqual("updatedValue", updatedValue);
            Assert.AreEqual("anotherValue", anotherValue2);
        }

        [Test]
        public void ShouldNotMergeOptionsToParentContext()
        {
            var context = new ContextBag();
            context.Set("someKey", "someValue");

            var parentContext = new FakeRootContext();

            new UnsubscribeContext(parentContext, typeof(object), context);

            var valueFound = parentContext.TryGet("someKey", out string _);

            Assert.IsFalse(valueFound);
        }

        [Test]
        public void ShouldExposeSendOptionsExtensionsAsOperationProperties()
        {
            var parentContext = new FakeRootContext(); // exact parent context doesn't matter
            var options = new ContextBag();
            options.Set("some key", "some value");

            var context = new UnsubscribeContext(parentContext, typeof(object), options);

            var operationProperties = context.GetOperationProperties();
            Assert.AreEqual("some value", operationProperties.Get<string>("some key"));
        }

        [Test]
        public void ShouldNotLeakParentsOperationProperties()
        {
            var outerOptions = new ContextBag();
            outerOptions.Set("outer key", "outer value");
            outerOptions.Set("shared key", "outer shared value");
            var parentContext = new UnsubscribeContext(new FakeRootContext(), typeof(object), outerOptions);

            var innerOptions = new ContextBag();
            innerOptions.Set("inner key", "inner value");
            innerOptions.Set("shared key", "inner shared value");
            var innerContext = new UnsubscribeContext(parentContext, typeof(object), innerOptions);

            var innerOperationProperties = innerContext.GetOperationProperties();
            Assert.AreEqual("inner value", innerOperationProperties.Get<string>("inner key"));
            Assert.AreEqual("inner shared value", innerOperationProperties.Get<string>("shared key"));
            Assert.IsFalse(innerOperationProperties.TryGet("outer key", out string _));

            var outerOperationProperties = parentContext.GetOperationProperties();
            Assert.AreEqual("outer value", outerOperationProperties.Get<string>("outer key"));
            Assert.AreEqual("outer shared value", outerOperationProperties.Get<string>("shared key"));
            Assert.IsFalse(outerOperationProperties.TryGet("inner key", out string _));
        }

        [Test]
        public void ShouldNotLeakParentsDeliveryConstraints()
        {
            var options = new ContextBag();
            var parentContext = new FakeRootContext();
            parentContext.Extensions.AddDeliveryConstraint(new NonDurableDelivery());

            var context = new UnsubscribeContext(parentContext, typeof(object), options);

            Assert.IsFalse(context.TryGetDeliveryConstraint(out NonDurableDelivery _));
        }
    }
}