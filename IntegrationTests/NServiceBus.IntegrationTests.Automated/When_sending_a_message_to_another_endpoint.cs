namespace NServiceBus.IntegrationTests.Automated
{
    using System.Collections.Generic;

    using EndpointTemplates;
    using NUnit.Framework;
    using Support;

    [TestFixture]
    public class When_sending_a_message_to_another_endpoint
    {
        [Test]
        public void Should_receive_the_message()
        {
            var context = new ReceiveContext();

            Scenario.For<AllTransports>()
                .WithEndpointBehaviour<SendBehavior>()
                .WithEndpointBehaviour<ReceiveBehavior>(context)
              .Run();

            Assert.True(context.WasCalled);
        }

        public class AllTransports : ScenarioDescriptor
        {
            public AllTransports()
            {
                this.Add(
                    new RunDescriptor
                        {
                            Name = "Msmq Transport",
                            Settings =
                                new Dictionary<string, string>
                                    {
                                        {
                                            "Transport",
                                            typeof(Msmq).AssemblyQualifiedName
                                        }
                                    }
                        });

                this.Add(
                    new RunDescriptor
                        {
                            Name = "ActiveMQ Transport",
                            Settings =
                                new Dictionary<string, string>
                                    {
                                        {
                                            "Transport",
                                            typeof(ActiveMQ).AssemblyQualifiedName
                                        }
                                    }
                        });
            }
        }

        public class ReceiveContext : BehaviorContext
        {
            public bool WasCalled { get; set; }
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
                this.context.WasCalled = true;
            }
        }
    }
}
