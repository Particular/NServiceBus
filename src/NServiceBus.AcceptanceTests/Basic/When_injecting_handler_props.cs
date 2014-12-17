namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_injecting_handler_props : NServiceBusAcceptanceTest
    {
        [Test]
        public void Run()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Receiver>(c=>c.When(b=>b.SendLocal(new MyMessage())))
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

                public IBus Bus { get; set; }

                public string Name { get; set; }

                public int Number { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.Number = Number;
                    Context.Name = Name;
                    Context.WasCalled = true;
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}
