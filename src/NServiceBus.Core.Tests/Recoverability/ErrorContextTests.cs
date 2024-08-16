namespace NServiceBus.Core.Tests.Recoverability;

using System;
using Extensibility;
using NUnit.Framework;
using Transport;

[TestFixture]
public class ErrorContextTests
{
    [Test]
    public void Can_pass_additional_information_via_context_bag()
    {
        var contextBag = new ContextBag();
        contextBag.Set("MyKey", "MyValue");
        var context = new ErrorContext(new Exception(), [], "ID", Array.Empty<byte>(), new TransportTransaction(), 0, "my-queue", contextBag);

        Assert.That(context.Extensions.Get<string>("MyKey"), Is.EqualTo("MyValue"));
    }
}