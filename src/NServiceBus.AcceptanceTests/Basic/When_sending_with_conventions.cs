namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_with_conventions : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_the_message()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<Endpoint>(b => b.Given((bus, context) => bus.SendLocal(new MyMessage
                    {
                        Id = context.Id
                    })))
                    .Done(c => c.WasCalled)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b => b.Conventions().DefiningMessagesAs(type => type == typeof(MyMessage)));
            }
        }

        [Serializable]
        public class MyMessage
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }


            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.WasCalled = true;
            }
        }
    }
}
