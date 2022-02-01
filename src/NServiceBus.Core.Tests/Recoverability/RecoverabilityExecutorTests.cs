namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class DiscardRecoverabilityActionTests
    {
        [Test]
        public void Discard_action_should_discard_message()
        {
            var discardAction = new Discard("not needed anymore");
            var errorContext = new ErrorContext(new Exception(""), new Dictionary<string, string>(), "some-id", new byte[0], new TransportTransaction(), 1, "my-endpoint", new ContextBag());

            var transportOperations = discardAction.Execute(errorContext, new Dictionary<string, string>());

            CollectionAssert.IsEmpty(transportOperations);
            Assert.AreEqual(discardAction.ErrorHandleResult, ErrorHandleResult.Handled);
        }
    }
}