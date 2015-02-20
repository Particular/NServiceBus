namespace NServiceBus.AcceptanceTests.MessageId
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;

    public class When_message_has_empty_id_header : NServiceBusAcceptanceTest
    {
        [Test]
        public void A_message_id_is_generated_by_the_transport_layer_on_receiving_side()
        {
            var context = new Context()
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.MessageReceived)
                    .Run();

            Assert.IsFalse(string.IsNullOrWhiteSpace(context.MessageId));
        }

        public class CorruptionMutator : IMutateOutgoingTransportMessages
        {
            public Context ScenarioContext { get; set; }

            public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
            {
                transportMessage.Headers["ScenarioContextId"] = ScenarioContext.Id.ToString();
                transportMessage.Headers[Headers.MessageId] = "";
            }
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool MessageReceived { get; set; }
            public string MessageId { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(busConfig =>
                {
                    busConfig.Pipeline.Register<InspectRawMessageStep.Registration>();
                    busConfig.RegisterComponents(c => c.ConfigureComponent<CorruptionMutator>(DependencyLifecycle.InstancePerCall));
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class InspectRawMessageStep : PhysicalMessageProcessingStageBehavior
            {
                public When_message_has_empty_id_header.Context ScenarioContext { get; set; }

                public override void Invoke(Context ctx, Action next)
                {
                    if (!ctx.PhysicalMessage.Headers.ContainsKey("ScenarioContextId"))
                    {
                        return;
                    }
                    var id = new Guid(ctx.PhysicalMessage.Headers["ScenarioContextId"]);
                    if (id != ScenarioContext.Id)
                    {
                        return;
                    }
                    ScenarioContext.MessageId = ctx.PhysicalMessage.Id;
                    ScenarioContext.MessageReceived = true;
                }

                public class Registration : RegisterStep
                {
                    public Registration()
                        : base("InspectRawMessageStep", typeof(InspectRawMessageStep), "Inspect if message has empty id")
                    {
                        InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
                    }
                }
            }


            class MessageSender : IWantToRunWhenBusStartsAndStops
            {
                readonly IBus bus;

                public MessageSender(IBus bus)
                {
                    this.bus = bus;
                }

                public void Start()
                {
                    bus.SendLocal(new Message());
                }

                public void Stop()
                {
                }
            }

            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
    
}