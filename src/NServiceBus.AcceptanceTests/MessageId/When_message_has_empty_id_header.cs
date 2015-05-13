namespace NServiceBus.AcceptanceTests.MessageId
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_message_has_empty_id_header : NServiceBusAcceptanceTest
    {
        [Test]
        public void A_message_id_is_generated_by_the_transport_layer_on_receiving_side()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.MessageReceived)
                    .Run();

            Assert.IsFalse(string.IsNullOrWhiteSpace(context.MessageId));
        }

        public class CorruptionBehavior : Behavior<DispatchContext>
        {
            public Context Context { get; set; }


            public override void Invoke(DispatchContext context, Action next)
            {
                context.Get<OutgoingMessage>().Headers["ScenarioContextId"] = Context.Id.ToString();
                context.Get<OutgoingMessage>().Headers[Headers.MessageId] = "";

                next();
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
                    busConfig.Pipeline.Register("CorruptionBehavior", typeof(CorruptionBehavior), "Blanks the message id");
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class InspectRawMessageStep : PhysicalMessageProcessingStageBehavior
            {
                public When_message_has_empty_id_header.Context ScenarioContext { get; set; }

                public override Task Invoke(Context ctx, Func<Task> next)
                {
                    if (!ctx.GetPhysicalMessage().Headers.ContainsKey("ScenarioContextId"))
                    {
                        return Task.FromResult(true);
                    }
                    var id = new Guid(ctx.GetPhysicalMessage().Headers["ScenarioContextId"]);
                    if (id != ScenarioContext.Id)
                    {
                        return Task.FromResult(true);
                    }
                    ScenarioContext.MessageId = ctx.GetPhysicalMessage().Id;
                    ScenarioContext.MessageReceived = true;

                    return Task.FromResult(true);
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
                IBus bus;

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