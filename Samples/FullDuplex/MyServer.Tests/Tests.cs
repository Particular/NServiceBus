using System;
using MyMessages;
using NUnit.Framework;
using NServiceBus.Testing;
using NServiceBus;

namespace MyServer.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestHandler()
        {
            Test.Initialize();

            var dataId = Guid.NewGuid();
            var str = "hello";
            WireEncryptedString secret = "secret";

            Test.Handler<RequestDataMessageHandler>()
                .SetIncomingHeader("Test", "abc")
                .ExpectReply<DataResponseMessage>(m => m.DataId == dataId && m.String == str && m.SecretAnswer == secret)
                .AssertOutgoingHeader("Test", "abc")
                .OnMessage<RequestDataMessage>(m => { m.DataId = dataId; m.String = str; m.SecretQuestion = secret; });
        }
    }
}
