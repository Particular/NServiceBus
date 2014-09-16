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
            Test.Initialize(configuration => configuration.Conventions().DefiningMessagesAs(t => t.Namespace == "Messages"));
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
