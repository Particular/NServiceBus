namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_TimeoutManager_is_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Bus_Defer_should_throw()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.RouteToThisEndpoint();

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.ExceptionThrown || c.SecondMessageReceived)
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferral>())
                .Should(c =>
                {
                    Assert.AreEqual(true, c.ExceptionThrown);
                    Assert.AreEqual(false, c.SecondMessageReceived);
                })
                .Run();
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
                public Context TestContext { get; set; }

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
                        TestContext.ExceptionThrown = true;
                    }
                }

                public Task Handle(MyOtherMessage message, IMessageHandlerContext context)
                {
                    TestContext.SecondMessageReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }

        [Serializable]
        public class MyOtherMessage : IMessage
        {
        }
    }
}