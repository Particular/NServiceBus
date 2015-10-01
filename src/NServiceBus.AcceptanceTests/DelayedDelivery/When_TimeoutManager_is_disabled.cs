namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_TimeoutManager_is_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Bus_Defer_should_throw()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.RouteToLocalEndpointInstance();

                        return bus.SendAsync(new MyMessage(), options);
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
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public async Task Handle(MyMessage message)
                {
                    try
                    {
                        var opts = new SendOptions();
                        opts.DelayDeliveryWith(TimeSpan.FromSeconds(5));
                        opts.RouteToLocalEndpointInstance();

                        await Bus.SendAsync(new MyOtherMessage(), opts);
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine(x.Message);
                        Context.ExceptionThrown = true;
                    }
                }

                public Task Handle(MyOtherMessage message)
                {
                    Context.SecondMessageReceived = true;

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
