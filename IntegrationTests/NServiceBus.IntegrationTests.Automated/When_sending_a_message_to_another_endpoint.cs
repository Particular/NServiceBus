namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using System.Collections.Generic;
    using EndpointTemplates;
    using NUnit.Framework;
    using Support;

    [TestFixture]
    [ForAllTransports]
    public class When_sending_a_message_to_another_endpoint
    {
        [Test]
        public void Should_receive_the_message()
        {
            //this sucks, we need a better way to assert on things
            MyMessageHandler.WasCalled = false;

            Scenario.Define()
                .WithEndpointBehaviour<SendBehaviour>()
                .WithEndpointBehaviour<ReceiveBehaviour>()
              .Run();
            
           
        }

        public class SendBehaviour : BehaviourFactory
        {
            public EndpointBehaviour Get()
            {
                return new ScenarioBuilder("Sender")
                    .EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>("Receiver")
                    .When(bus => bus.Send(new MyMessage()))
                    .CreateScenario();
            }
        }

        public class ReceiveBehaviour : BehaviourFactory
        {
            public EndpointBehaviour Get()
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

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public static bool WasCalled;

            public void Handle(MyMessage message)
            {
                WasCalled = true;
            }
        }
    }

    public class Scenario
    {
        readonly IList<BehaviourFactory> behaviours = new List<BehaviourFactory>();

        public static Scenario Define()
        {
            return new Scenario();
        }

        public  Scenario WithEndpointBehaviour<T>() where T:BehaviourFactory
        {
            behaviours.Add(Activator.CreateInstance<T>() as BehaviourFactory);
            return this;
        }


        public void Run()
        {
            ScenarioRunner.Run(behaviours);
        }
    }
}
