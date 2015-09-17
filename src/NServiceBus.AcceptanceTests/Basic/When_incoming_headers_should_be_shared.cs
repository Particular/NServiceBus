namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_incoming_headers_should_be_shared : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_expose_header_in_downstream_handlers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocalAsync(new Message())))
                .Done(c => c.GotMessage)
                .Run();

            Assert.IsTrue(context.SecondHandlerCanReadHeaderSetByFirstHandler);
        }

        public class Context : ScenarioContext
        {
            public bool SecondHandlerCanReadHeaderSetByFirstHandler { get; set; }
            public bool GotMessage   { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.ExecuteTheseHandlersFirst(typeof(FirstHandler), typeof(SecondHandler)));
            }

            class FirstHandler : IHandleMessages<Message>
            {
                public IBus Bus { get; set; }

                public Task Handle(Message message)
                {
                    Bus.CurrentMessageContext.Headers["Key"] = "Value";

                    return Task.FromResult(0);
                }
            }

            class SecondHandler : IHandleMessages<Message>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public Task Handle(Message message)
                {
                    var header = Bus.CurrentMessageContext.Headers["Key"];
                    Context.SecondHandlerCanReadHeaderSetByFirstHandler = header == "Value";
                    Context.GotMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class Message : ICommand
        {
        }

    }
}