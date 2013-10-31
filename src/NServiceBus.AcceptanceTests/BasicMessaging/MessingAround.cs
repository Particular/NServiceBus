namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using System.Collections.Generic;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class MessingAround : NServiceBusAcceptanceTest
    {
        [Test, Ignore("Just added for convenience")]
        public void CanExecuteEntirePipelineInRealisticScenario()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<Sender>(b => b.Given((bus, context) => bus.Send(new MyMessage { Id = context.Id })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Serializers.Xml)
                                  .For(Builders.Windsor))
                    .Should(c =>
                                {
                                    Assert.True(c.WasCalled, "The message handler should be called");
                                    Assert.AreEqual(1, c.TimesCalled, "The message handler should only be invoked once");
                                    Assert.AreEqual(Environment.MachineName, c.ReceivedHeaders[Headers.OriginatingMachine], "The sender should attach the machine name as a header");
                                    Assert.True(c.ReceivedHeaders[Headers.OriginatingEndpoint].Contains("Sender"), "The sender should attach its endpoint name as a header");
                                    Assert.AreEqual(Environment.MachineName, c.ReceivedHeaders[Headers.ProcessingMachine], "The receiver should attach the machine name as a header");
                                    Assert.True(c.ReceivedHeaders[Headers.ProcessingEndpoint].Contains("Receiver"), "The receiver should attach its endpoint name as a header");
                                })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public int TimesCalled { get; set; }
            public IDictionary<string, string> ReceivedHeaders { get; set; }
            public Guid Id { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
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

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.TimesCalled++;

                Context.ReceivedHeaders = Bus.CurrentMessageContext.Headers;

                Context.WasCalled = true;
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}