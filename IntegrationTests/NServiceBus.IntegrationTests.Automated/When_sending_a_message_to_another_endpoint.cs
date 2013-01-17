using System.Collections.Generic;
using System.Text;

namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using Config;
    using NUnit.Framework;

    [TestFixture]
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
                .AddMapping<MyMessage>("Receiver")
                 .Given(bus=> bus.Send(new MyMessage()))
                 .CreateScenario();
            }
        }

        public class ReceiveScenario:IScenarioFactory
        {
            public EndpointScenario Get()
            {
                return new ScenarioBuilder("Receiver")
                 .RegisterHandler<MyMessageHandler>()
                 .Done(()=>MyMessageHandler.Called)
                 .Assert(() =>
                 {
                     Assert.True(MyMessageHandler.Called);
                 })
                 .CreateScenario();
            }
        }



        public class MyMessage:IMessage
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

    public interface IScenarioFactory

    {
        EndpointScenario Get();
    }

    public class EndpointScenario
    {
        public string EndpointName { get; set; }

        public IList<Action<IBus>> InitialBusActions { get; set; }

        public MessageEndpointMappingCollection EndpointMappings { get; set; }

        public Func<bool> Done { get; set; }
    }
}
