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
                 .Done(() => MyMessageHandler.Called)
                 .Assert(() => Assert.Fail("The y was wrong"))

                 .CreateScenario();
            }
        }



        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public static bool Called;

            public void Handle(MyMessage message)
            {
                Called = true;
            }
        }
    }
}
