namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_multiple_mappings_exists : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task First_registration_should_be_the_final_destination()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send(new MyCommand1())))
                    .WithEndpoint<Receiver1>()
                    .WithEndpoint<Receiver2>()
                    .Done(c => c.WasCalled1 || c.WasCalled2)
                    .Run();

            Assert.IsTrue(context.WasCalled1);
            Assert.IsFalse(context.WasCalled2);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled1 { get; set; }
            public bool WasCalled2 { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyCommand1>(typeof(Receiver1))
                    .AddMapping<MyCommand2>(typeof(Receiver2));
            }
        }

        public class Receiver1 : EndpointConfigurationBuilder
        {
            public Receiver1()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyBaseCommand>
            {
                public Context Context { get; set; }

                public Task Handle(MyBaseCommand message, IMessageHandlerContext context)
                {
                    Context.WasCalled1 = true;
                    return Task.Delay(2000); // Just to be sure the other receiver is finished
                }
            }
        }

        public class Receiver2 : EndpointConfigurationBuilder
        {
            public Receiver2()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyBaseCommand>
            {
                public Context Context { get; set; }

                public Task Handle(MyBaseCommand message, IMessageHandlerContext context)
                {
                    Context.WasCalled2 = true;
                    return Task.Delay(2000); // Just to be sure the other receiver is finished
                }
            }
        }

        [Serializable]
        public abstract class MyBaseCommand : ICommand
        {
        }

        [Serializable]
        public class MyCommand1 : MyBaseCommand
        {
        }

        [Serializable]
        public class MyCommand2 : MyBaseCommand
        {
        }
    }
}
