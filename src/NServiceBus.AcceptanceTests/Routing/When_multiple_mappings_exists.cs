namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_multiple_mappings_exists : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task First_registration_should_be_the_final_destination()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((session, c) => session.Send(new MyCommand1())))
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
                EndpointSetup<DefaultServer>(c =>
                {
                    var routing = c.ConfigureTransport().Routing();
                    routing.RouteToEndpoint(typeof(MyCommand1), typeof(Receiver1));
                    routing.RouteToEndpoint(typeof(MyCommand2), typeof(Receiver2));
                });
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
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyBaseCommand message, IMessageHandlerContext context)
                {
                    testContext.WasCalled1 = true;
                    return Task.Delay(2000); // Just to be sure the other receiver is finished
                }

                Context testContext;
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
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyBaseCommand message, IMessageHandlerContext context)
                {
                    testContext.WasCalled2 = true;
                    return Task.Delay(2000); // Just to be sure the other receiver is finished
                }

                Context testContext;
            }
        }


        public abstract class MyBaseCommand : ICommand
        {
        }


        public class MyCommand1 : MyBaseCommand
        {
        }


        public class MyCommand2 : MyBaseCommand
        {
        }
    }
}