using System.Text;

namespace NServiceBus.IntegrationTests.Automated
{
    using EndpointTemplates;
    using NUnit.Framework;
    using Support;

    [TestFixture]
    //[ForAllTransports(Except="RabbitMQ")]
    public class When_sending_a_message_to_another_endpoint
    {
        [Test]
        public void Should_receive_the_message()
        {
            MyMessageHandler.WasCalled = false;
            ScenarioRunner.Run(new SendScenario(), new ReceiveScenario());
        }


        [Test]
        public void Should_receive_the_message_2()
        {
            MyMessageHandler.WasCalled = false;
            ScenarioRunner.Run(new SendScenario(), new ReceiveScenario());
        }


        public class SendScenario : IScenarioFactory
        {
            public EndpointScenario Get()
            {
                return new ScenarioBuilder("Sender")
                    .EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>("Receiver")
                    .When(bus => bus.Send(new MyMessage()))
                    .CreateScenario();
            }
        }

        public class ReceiveScenario : IScenarioFactory
        {
            public EndpointScenario Get()
            {

                return new ScenarioBuilder("Receiver")
                 .EndpointSetup<DefaultServer>()
                 .Done(() => MyMessageHandler.WasCalled)
                 .Assert(() => Assert.True(MyMessageHandler.WasCalled))

                 .CreateScenario();
            }
        }



        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler :IHandleMessages<MyMessage>
        {
            public static bool WasCalled;
            public void Handle(MyMessage message)
            {
                WasCalled = true;
            }
        }
    }
}
