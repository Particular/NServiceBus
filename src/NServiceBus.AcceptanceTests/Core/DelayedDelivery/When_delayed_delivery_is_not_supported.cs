namespace NServiceBus.AcceptanceTests.Core.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_delayed_delivery_is_not_supported : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Trying_to_delay_should_throw()
        {
            if (TestSuiteConstraints.Current.SupportsDelayedDelivery)
            {
                Assert.Ignore("Ignoring this test because it requires the transport to not support delayed delivery.");
            }

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

        public class Endpoint : EndpointFromTemplate<DefaultServer>
        {
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