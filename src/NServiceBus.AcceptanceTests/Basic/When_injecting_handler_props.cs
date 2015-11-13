namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_injecting_handler_props : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Run()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Receiver>(c=>c.When(b => b.SendLocal(new MyMessage())))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.AreEqual(10, context.Number);
            Assert.AreEqual("Foo", context.Name);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string Name { get; set; }
            public int Number { get; set; }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.InitializeHandlerProperty<MyMessageHandler>("Number", 10);
                    c.InitializeHandlerProperty<MyMessageHandler>("Name", "Foo");
                });

            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public string Name { get; set; }

                public int Number { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.Number = Number;
                    Context.Name = Name;
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}
