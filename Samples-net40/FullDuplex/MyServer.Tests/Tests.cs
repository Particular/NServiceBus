using System;
using MyMessages;
using NUnit.Framework;
using NServiceBus.Testing;

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

            Test.Handler<RequestDataMessageHandler>()
                .SetIncomingHeader("Test", "abc")
                .ExpectReply<DataResponseMessage>(m => m.DataId == dataId && m.String == str)
                .AssertOutgoingHeader("Test", "abc")
                .OnMessage<RequestDataMessage>(m => { m.DataId = dataId; m.String = str; });
        }
    }
}
