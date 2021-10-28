namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
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
            var context = new ErrorContext(new Exception(), new Dictionary<string, string>(), "ID", new byte[0], new TransportTransaction(), 0, "my-queue", contextBag);

            Assert.AreEqual("MyValue", context.Extensions.Get<string>("MyKey"));
        }
    }
}