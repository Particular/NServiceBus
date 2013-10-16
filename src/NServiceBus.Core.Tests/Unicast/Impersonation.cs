namespace NServiceBus.Unicast.Tests
{
    using System.Security.Principal;
    using System.Threading;
    using Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class When_windows_impersonation_is_enabled : using_the_unicastBus
    {
        [Test]
        public void Should_impersonate_the_client()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();

            ConfigureImpersonation.RunHandlersUnderIncomingPrincipal(null, true);

            RegisterMessageHandlerType<MyHandler>();
            receivedMessage.Headers[Headers.WindowsIdentityName] = "TestUser";
            ReceiveMessage(receivedMessage);


            Assert.AreEqual("TestUser", MyHandler.Principal.Identity.Name);
        }

        class MyHandler:IHandleMessages<EventMessage>
        {
            public static IPrincipal Principal { get; set; }
            public void Handle(EventMessage message)
            {

                Principal = Thread.CurrentPrincipal;
            }
        }
    }
}