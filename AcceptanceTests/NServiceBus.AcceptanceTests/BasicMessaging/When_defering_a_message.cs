namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_defering_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            Scenario.Define(() => new Context())
                    .WithEndpoint<EndPoint>(b => b.Given((bus, context) => bus.Defer(TimeSpan.FromMilliseconds(10), new MyMessage())))
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c =>
                        {
                            Assert.True(c.WasCalled, "The message handler should be called");
                            Assert.AreEqual(1, c.TimesCalled, "The message handler should only be invoked once");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }

            public int TimesCalled { get; set; }
        }

        public class EndPoint : EndpointConfigurationBuilder
        {
            public EndPoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.TimesCalled++;
                    Context.WasCalled = true;
                }
            }
        }


        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }

    }
}
