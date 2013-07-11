namespace Tests
{
    using System;
    using Messages;
    using NServiceBus;
    using NServiceBus.Testing;
    using NUnit.Framework;
    using Server;

    [TestFixture]
    public class RequestMessageHandlerShould
    {
        [TestFixtureSetUp]
        public void TestSetup()
        {
            MessageConventionExtensions.IsMessageTypeAction =
                t => t.Namespace != null && t.Namespace.EndsWith("Messages") && !t.Namespace.StartsWith("NServiceBus");

            Test.Initialize();
        }

        [Test]
        public void ReplyWithResponseIdEqualToRequestId()
        {
            var requestId = Guid.NewGuid();

            Test.Handler<RequestMessageHandler>()
                .ExpectReply<Response>(m => m.ResponseId == requestId)
                .OnMessage<Request>(m => m.RequestId = requestId);
        }
    }
}
