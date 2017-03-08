// disable obsolete warnings. Tests will be removed in next major version
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.Core.DelayedDelivery.TimeoutManager
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_using_external_timeout_manager_xml_config : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_delay_delivery()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithTimeoutManager>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromDays(5));
                    options.RouteToThisEndpoint();

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.WasCalled)
                .Run();

            Assert.IsTrue(context.TimeoutManagerHeaderDetected);
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
#pragma warning restore CS0618
