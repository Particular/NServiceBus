namespace NServiceBus.Core.Tests.Routing;

using System;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class SubscribeContextTests
{
    [Test]
    public void ShouldShallowCloneContextBag()
    {
        var context = new ContextBag();
        context.Set("someKey", "someValue");

        var testee = new SubscribeContext(new FakeRootContext(), Type.EmptyTypes, context);
        testee.Extensions.Set("someKey", "updatedValue");
        testee.Extensions.Set("anotherKey", "anotherValue");
        context.TryGet("someKey", out string value);
        Assert.That(value, Is.EqualTo("someValue"));
        Assert.That(context.TryGet("anotherKey", out string _), Is.False);
        testee.Extensions.TryGet("someKey", out string updatedValue);
        testee.Extensions.TryGet("anotherKey", out string anotherValue2);
        Assert.That(updatedValue, Is.EqualTo("updatedValue"));
        Assert.That(anotherValue2, Is.EqualTo("anotherValue"));
    }

    [Test]
    public void ShouldNotMergeOptionsToParentContext()
    {
        var context = new ContextBag();
        context.Set("someKey", "someValue");

        var parentContext = new FakeRootContext();

        _ = new SubscribeContext(parentContext, Type.EmptyTypes, context);

        var valueFound = parentContext.TryGet("someKey", out string _);

        Assert.That(valueFound, Is.False);
    }

    [Test]
    public void ShouldExposeSendOptionsExtensionsAsOperationProperties()
    {
        var parentContext = new FakeRootContext(); // exact parent context doesn't matter
        var options = new ContextBag();
        options.Set("some key", "some value");

        var context = new SubscribeContext(parentContext, Type.EmptyTypes, options);

        var operationProperties = context.GetOperationProperties();
        Assert.That(operationProperties.Get<string>("some key"), Is.EqualTo("some value"));
    }

    [Test]
    public void ShouldNotLeakParentsOperationProperties()
    {
        var outerOptions = new ContextBag();
        outerOptions.Set("outer key", "outer value");
        outerOptions.Set("shared key", "outer shared value");
        var parentContext = new SubscribeContext(new FakeRootContext(), Type.EmptyTypes, outerOptions);

        var innerOptions = new ContextBag();
        innerOptions.Set("inner key", "inner value");
        innerOptions.Set("shared key", "inner shared value");
        var innerContext = new SubscribeContext(parentContext, Type.EmptyTypes, innerOptions);

        var innerOperationProperties = innerContext.GetOperationProperties();
        Assert.That(innerOperationProperties.Get<string>("inner key"), Is.EqualTo("inner value"));
        Assert.That(innerOperationProperties.Get<string>("shared key"), Is.EqualTo("inner shared value"));
        Assert.That(innerOperationProperties.TryGet("outer key", out string _), Is.False);

        var outerOperationProperties = parentContext.GetOperationProperties();
        Assert.That(outerOperationProperties.Get<string>("outer key"), Is.EqualTo("outer value"));
        Assert.That(outerOperationProperties.Get<string>("shared key"), Is.EqualTo("outer shared value"));
        Assert.That(outerOperationProperties.TryGet("inner key", out string _), Is.False);
    }
}