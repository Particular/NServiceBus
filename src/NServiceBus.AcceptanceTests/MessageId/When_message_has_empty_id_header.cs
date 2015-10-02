namespace NServiceBus.AcceptanceTests.MessageId
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Config;
    using Pipeline;
    using TransportDispatch;
    using NUnit.Framework;

    public class When_message_has_empty_id_header : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task A_message_id_is_generated_by_the_transport_layer_on_receiving_side()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.MessageReceived)
                    .Run();

            Assert.IsFalse(string.IsNullOrWhiteSpace(context.MessageId));
        }

        public class CorruptionBehavior : Behavior<RoutingContext>
        {
            public Context Context { get; set; }
            
            public override Task Invoke(RoutingContext context, Func<Task> next)
            {
                context.Message.Headers["ScenarioContextId"] = Context.Id.ToString();
                context.Message.Headers[Headers.MessageId] = "";

                return next();
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

            class InspectRawMessageStep : Behavior<PhysicalMessageProcessingContext>
            {
                public Context ScenarioContext { get; set; }

                public override Task Invoke(PhysicalMessageProcessingContext ctx, Func<Task> next)
                {
                    if (!ctx.Message.Headers.ContainsKey("ScenarioContextId"))
                    {
                        return Task.FromResult(0);
                    }
                    var id = new Guid(ctx.Message.Headers["ScenarioContextId"]);
                    if (id != ScenarioContext.Id)
                    {
                        return Task.FromResult(0);
                    }
                    ScenarioContext.MessageId = ctx.Message.Id;
                    ScenarioContext.MessageReceived = true;

                    return Task.FromResult(0);
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

                public Task StartAsync()
                {
                    return bus.SendLocalAsync(new Message());
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }

            class Handler : IHandleMessages<Message>
            {
                public Task Handle(Message message)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }

}