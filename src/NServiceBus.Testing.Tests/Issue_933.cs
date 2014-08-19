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
            public IBus Bus { get; set; }

            public void Handle(MyCommand message)
            {
                message.Header1 = Bus.GetMessageHeader(message, "Key1");
                message.Header2 = Bus.GetMessageHeader(message, "Key2");
            }
        }
    }
}
