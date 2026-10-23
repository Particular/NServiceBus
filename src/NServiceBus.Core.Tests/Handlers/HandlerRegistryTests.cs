namespace NServiceBus.Core.Tests.Handlers;

using System;
using NUnit.Framework;

[TestFixture]
public class HandlerRegistryTests
{
    [Test]
    public void AddAll_should_throw_when_not_replaced_by_the_source_generator()
    {
        var registry = new HandlerRegistry(new EndpointConfiguration("HandlerRegistryTests"));

        var exception = Assert.Throws<InvalidOperationException>(registry.AddAll);

        Assert.That(exception.Message, Does.Contain("HandlerRegistry.AddAll()").And.Contain("source generator"));
    }

    [Test]
    public void AddAllHandlers_should_throw_when_not_replaced_by_the_source_generator()
    {
        var registry = new HandlerRegistry(new EndpointConfiguration("HandlerRegistryTests"));

        var exception = Assert.Throws<InvalidOperationException>(registry.AddAllHandlers);

        Assert.That(exception.Message, Does.Contain("HandlerRegistry.AddAllHandlers()").And.Contain("source generator"));
    }

    [Test]
    public void AddAllSagas_should_throw_when_not_replaced_by_the_source_generator()
    {
        var registry = new HandlerRegistry(new EndpointConfiguration("HandlerRegistryTests"));

        var exception = Assert.Throws<InvalidOperationException>(registry.AddAllSagas);

        Assert.That(exception.Message, Does.Contain("HandlerRegistry.AddAllSagas()").And.Contain("source generator"));
    }
}
