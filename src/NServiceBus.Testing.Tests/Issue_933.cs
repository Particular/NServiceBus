namespace NServiceBus.Testing.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class Issue_933 : BaseTests
    {
        [Test]
        public void SendMessageWithMultiIncomingHeaders()
        {
            var command = new MyCommand();

            Test.Handler<MyCommandHandler>()
                .SetIncomingHeader("Key1", "Header1")
                .SetIncomingHeader("Key2", "Header2")
                .OnMessage(command);

            Assert.AreEqual("Header1", command.Header1);
            Assert.AreEqual("Header2", command.Header2);
        }

        public class MyCommand : ICommand
        {
            public string Header1 { get; set; }
            public string Header2 { get; set; }
        }

        public class MyCommandHandler : IHandleMessages<MyCommand>
        {
            public void Handle(MyCommand message)
            {
                message.Header1 = message.GetHeader("Key1");
                message.Header2 = message.GetHeader("Key2");
            }
        }
    }
}
