namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_external_timeout_manager : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_delay_delivery()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithTimeoutManager>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromDays(5));
                    options.RouteToThisEndpoint();

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.WasCalled)
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferral>())
                .Should(c => { Assert.IsTrue(c.TimeoutManagerHeaderDetected); })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool TimeoutManagerHeaderDetected { get; set; }
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                var address = Conventions.EndpointNamingConvention(typeof(EndpointWithTimeoutManager));

                EndpointSetup<DefaultServer>(config => config.DisableFeature<TimeoutManager>())
                    .WithConfig<UnicastBusConfig>(c => { c.TimeoutManagerAddress = address; });
            }
        }

        public class EndpointWithTimeoutManager : EndpointConfigurationBuilder
        {
            public EndpointWithTimeoutManager()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.TimeoutManagerHeaderDetected = context.MessageHeaders.ContainsKey("NServiceBus.Timeout.Expire");
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}
