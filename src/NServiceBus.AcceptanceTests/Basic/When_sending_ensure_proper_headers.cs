namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    class When_sending_ensure_proper_headers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_have_proper_headers_for_the_originating_endpoint()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                                .WithEndpoint<Sender>(b => b.Given((bus, ctx) => bus.Send<MyMessage>(m =>
                                {
                                    m.Id = ctx.Id;
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
            public IDictionary<string, string> ReceivedHeaders { get; set; }
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
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.ReceivedHeaders = Bus.CurrentMessageContext.Headers;
                Context.WasCalled = true;
            }
        }
    }
}
