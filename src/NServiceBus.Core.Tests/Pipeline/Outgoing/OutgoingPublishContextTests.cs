namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using System.Collections.Generic;
    using Extensibility;
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

            var testee = new OutgoingPublishContext(message, "message-id", options.OutgoingHeaders, options.Context, new FakeRootContext());
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

            var parentContext = new FakeRootContext();

            new OutgoingPublishContext(message, "message-id", options.OutgoingHeaders, options.Context, parentContext);

            var valueFound = parentContext.TryGet("someKey", out string _);

            Assert.IsFalse(valueFound);
        }

        [Test]
        public void ShouldExposePublishOptionsExtensionsAsOperationProperties()
        {
            var message = new OutgoingLogicalMessage(typeof(object), new object());
            var parentContext = new FakeRootContext(); // exact parent context doesn't matter
            var options = new ContextBag();
            options.Set("some key", "some value");

            var publishContext = new OutgoingPublishContext(message, "message-id", new Dictionary<string, string>(), options, parentContext);

            var operationProperties = publishContext.GetOperationProperties();
            Assert.AreEqual("some value", operationProperties.Get<string>("some key"));
        }

        [Test]
        public void ShouldNotLeakParentsOperationProperties()
        {
            var outerOptions = new ContextBag();
            outerOptions.Set("outer key", "outer value");
            outerOptions.Set("shared key", "outer shared value");
            var parentContext = new OutgoingPublishContext(new OutgoingLogicalMessage(typeof(object), new object()), "message-id", new Dictionary<string, string>(), outerOptions, new FakeRootContext());

            var innerOptions = new ContextBag();
            innerOptions.Set("inner key", "inner value");
            innerOptions.Set("shared key", "inner shared value");
            var innerContext = new OutgoingPublishContext(new OutgoingLogicalMessage(typeof(object), new object()), "message-id", new Dictionary<string, string>(), innerOptions, parentContext);

            var innerOperationProperties = innerContext.GetOperationProperties();
            Assert.AreEqual("inner value", innerOperationProperties.Get<string>("inner key"));
            Assert.AreEqual("inner shared value", innerOperationProperties.Get<string>("shared key"));
            Assert.IsFalse(innerOperationProperties.TryGet("outer key", out string _));

            var outerOperationProperties = parentContext.GetOperationProperties();
            Assert.AreEqual("outer value", outerOperationProperties.Get<string>("outer key"));
            Assert.AreEqual("outer shared value", outerOperationProperties.Get<string>("shared key"));
            Assert.IsFalse(outerOperationProperties.TryGet("inner key", out string _));
        }
    }
}