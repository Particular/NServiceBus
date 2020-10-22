namespace NServiceBus.AcceptanceTests.Core.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_ensure_proper_headers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_have_proper_headers_for_the_originating_endpoint()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Sender>(b => b.When((session, c) => session.Send<MyMessage>(m => { m.Id = c.Id; })))
                .WithEndpoint<Receiver>()
                .Done(c => c.WasCalled)
                .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
            Assert.AreEqual("SenderForEnsureProperHeadersTest", context.ReceivedHeaders[Headers.OriginatingEndpoint], "Message should contain the Originating endpoint");
            Assert.That(context.ReceivedHeaders[Headers.OriginatingHostId], Is.Not.Null.Or.Empty, "OriginatingHostId cannot be null or empty");
            Assert.That(context.ReceivedHeaders[Headers.OriginatingMachine], Is.Not.Null.Or.Empty, "Endpoint machine name cannot be null or empty");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public IReadOnlyDictionary<string, string> ReceivedHeaders { get; set; }
            public Guid Id { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                CustomEndpointName("SenderForEnsureProperHeadersTest");
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Routing().RouteToEndpoint(typeof(MyMessage), typeof(Receiver));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (testContext.Id != message.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.ReceivedHeaders = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                testContext.WasCalled = true;

                return Task.FromResult(0);
            }

            Context testContext;
        }
    }
}