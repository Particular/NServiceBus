namespace NServiceBus.AcceptanceTests.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_ensure_proper_headers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_have_proper_headers_for_the_originating_endpoint()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                                .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send<MyMessage>(m =>
                                {
                                    m.Id = c.Id;
                                })))
                                .WithEndpoint<Receiver>()
                                .Done(c => c.WasCalled)
                                .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
            Assert.AreEqual("SenderForEnsureProperHeadersTest", context.ReceivedHeaders[Headers.OriginatingEndpoint], "Message should contain the Originating endpoint");
            Assert.IsNotNullOrEmpty(context.ReceivedHeaders[Headers.OriginatingHostId], "OriginatingHostId cannot be null or empty");
            Assert.IsNotNullOrEmpty(context.ReceivedHeaders[Headers.OriginatingMachine], "Endpoint machine name cannot be null or empty");
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
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context TestContext { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (TestContext.Id != message.Id)
                    return Task.FromResult(0);

                TestContext.ReceivedHeaders = context.MessageHeaders;
                TestContext.WasCalled = true;

                return Task.FromResult(0);
            }
        }
    }
}
