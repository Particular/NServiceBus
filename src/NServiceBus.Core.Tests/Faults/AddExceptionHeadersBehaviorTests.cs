namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class AddExceptionHeadersBehaviorTests
    {
        [Test]
        public async Task ShouldEnrichHeadersWithExceptionDetails()
        {
            var sourceQueue = "public-receive-address";
            var exception = new Exception("exception-message");
            var messageId = "message-id";

            var context = CreateContext(messageId, sourceQueue, exception);
            var behavior = CreateBehavior();

            await behavior.Invoke(context, c => Task.FromResult(0));

            Assert.AreEqual("public-receive-address", context.Message.Headers[FaultsHeaderKeys.FailedQ]);
            Assert.AreEqual("exception-message", context.Message.Headers["NServiceBus.ExceptionInfo.Message"]);
        }

        static IFaultContext CreateContext(string messageId, string sourceQueue, Exception exception)
        {
            var context = new FaultContext(
                new OutgoingMessage(messageId, new Dictionary<string, string>(), new byte[0]), 
                sourceQueue, 
                exception, 
                null);

            return context;
        }

        AddExceptionHeadersBehavior CreateBehavior()
        {
            var behavior = new AddExceptionHeadersBehavior();

            return behavior;
        }
    }
}