namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_assembly_level_message_mapping_for_pub_sub : NServiceBusAcceptanceTest
    {
        static string OtherEndpointName => Conventions.EndpointNamingConvention(typeof(OtherEndpoint));

        [Test]
        public async Task The_mapping_should_not_cause_publishing_to_non_subscribers()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<OtherEndpoint>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.EndpointsStarted, async session =>
                    {
                        await session.Publish(new MyEvent());
                        await session.Send(new DoneCommand());
                    })
                )
                .Done(c => c.Done)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Run();
        }

        public class Context : ScenarioContext
        {
            public int HandlerInvoked { get; set; }
            public bool Subscribed { get; set; }
            public bool Done { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; }))
                    .WithConfig<UnicastBusConfig>(c =>
                    {
                        c.MessageEndpointMappings = new MessageEndpointMappingCollection();
                        c.MessageEndpointMappings.Add(new MessageEndpointMapping
                        {
                            Endpoint = OtherEndpointName,
                            AssemblyName = typeof(Publisher).Assembly.GetName().Name
                        });
                    })
                    .AddMapping<DoneCommand>(typeof(OtherEndpoint));
            }
        }

        public class OtherEndpoint : EndpointConfigurationBuilder
        {
            public OtherEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.LimitMessageProcessingConcurrencyTo(1);
                });
            }

            public class DoneHandler : IHandleMessages<DoneCommand>
            {
                public Context Context { get; set; }

                public Task Handle(DoneCommand message, IMessageHandlerContext context)
                {
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }
        
        public class MyEvent : IEvent
        {
        }
        
        public class DoneCommand : ICommand
        {
            
        }
    }
}