namespace NServiceBus.AcceptanceTests.Core.DelayedDelivery.TimeoutManager
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_TimeoutManager_is_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Bus_Defer_should_throw()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.RouteToThisEndpoint();

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.ExceptionThrown || c.SecondMessageReceived)
                .Run();

            Assert.AreEqual(true, context.ExceptionThrown);
            Assert.AreEqual(false, context.SecondMessageReceived);

        }

        public class Context : ScenarioContext
        {
            public bool ExceptionThrown { get; set; }
            public bool SecondMessageReceived { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                // Explicitly disable TimeoutManager, although this should be default anyway
                EndpointSetup<DefaultServer>(config => config.DisableFeature<TimeoutManager>());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>, IHandleMessages<MyOtherMessage>
            {
                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    try
                    {
                        var opts = new SendOptions();
                        opts.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                        opts.RouteToThisEndpoint();

                        await context.Send(new MyOtherMessage(), opts);
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine(x.Message);
                        testContext.ExceptionThrown = true;
                    }
                }

                public Task Handle(MyOtherMessage message, IMessageHandlerContext context)
                {
                    testContext.SecondMessageReceived = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class MyMessage : IMessage
        {
        }


        public class MyOtherMessage : IMessage
        {
        }
    }
}