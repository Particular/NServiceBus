namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Support;

    [TestFixture]
    public class When_sending_a_message_to_another_endpoint
    {
        [Test]
        public void Should_receive_the_message()
        {
            Scenario.Define()
                .WithEndpointBehaviour<SendBehavior>()
                .WithEndpointBehaviour<ReceiveBehavior>(() => new ReceiveContext())
                .Repeat(r => r.For<AllTransports>().Except("ActiveMQ")
                         .For<AllSerializers>()
                         .For<AllBuilders>())
                .Should<ReceiveContext>(c =>
                    {
                        Assert.True(c.WasCalled);
                        Assert.AreEqual(1, c.TimesCalled);
                    })
                .Run();
        }
     
        public class ReceiveContext : BehaviorContext
        {
            public bool WasCalled { get; set; }

            public int TimesCalled { get; set; }
        }

        public class SendBehavior : BehaviorFactory
        {
            public EndpointBehavior Get()
            {
                return new ScenarioBuilder("Sender")
                    .EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>("Receiver")
                    .When(bus => bus.Send(new MyMessage()))
                    .CreateScenario();
            }
        }

        public class ReceiveBehavior : BehaviorFactory
        {
            public EndpointBehavior Get()
            {
                return new ScenarioBuilder("Receiver")
                    .EndpointSetup<DefaultServer>()
                    .Done((ReceiveContext context) => context.WasCalled)
                    .CreateScenario();
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            private readonly ReceiveContext context;

            public MyMessageHandler(ReceiveContext context)
            {
                this.context = context;
            }

            public void Handle(MyMessage message)
            {
                context.WasCalled = true;
                context.TimesCalled++;
            }
        }
    }
}
