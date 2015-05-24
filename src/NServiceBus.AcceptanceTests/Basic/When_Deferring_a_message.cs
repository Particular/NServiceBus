﻿namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_Deferring_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Message_should_be_received()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new MyMessage(), new SendLocalOptions(delayDeliveryFor: TimeSpan.FromSeconds(3)))))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.IsTrue(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
            public class MyMessageHandler : IProcessCommands<MyMessage>
            {
                public Context Context { get; set; }

                public void Handle(MyMessage message, ICommandContext context)
                {
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
