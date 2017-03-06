// disable obsolete warnings. Test will be removed in next major version
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_sending_a_base_command : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Route_for_derived_command_should_be_used()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(c => c.EndpointsStarted, async session =>
                {
                    await session.Send(new BaseCommand());
                }))
                .WithEndpoint<Receiver>()
                .WithEndpoint<QueueSpy>()
                .Done(c => c.UsedBaseRoute || c.UsedDerivedRoute)
                .Run();

            Assert.True(context.UsedDerivedRoute);
            Assert.False(context.UsedBaseRoute);
        }

        public class Context : ScenarioContext
        {
            public bool UsedDerivedRoute { get; set; }
            public bool UsedBaseRoute { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<UnicastBusConfig>(c =>
                    {
                        // This scenario is only supported when using MessageEndpointMappings
                        c.MessageEndpointMappings.Add(new MessageEndpointMapping
                        {
                            AssemblyName = typeof(BaseCommand).Assembly.FullName,
                            TypeFullName = typeof(BaseCommand).FullName,
                            Endpoint = Conventions.EndpointNamingConvention(typeof(QueueSpy))
                        });
                        c.MessageEndpointMappings.Add(new MessageEndpointMapping
                        {
                            AssemblyName = typeof(DerivedCommand).Assembly.FullName,
                            TypeFullName = typeof(DerivedCommand).FullName,
                            Endpoint = Conventions.EndpointNamingConvention(typeof(Receiver))
                        });
                    });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyEventHandler : IHandleMessages<BaseCommand>
            {
                public Context Context { get; set; }

                public Task Handle(BaseCommand messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.UsedDerivedRoute = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class QueueSpy : EndpointConfigurationBuilder
        {
            public QueueSpy()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyEventHandler : IHandleMessages<BaseCommand>
            {
                public Context Context { get; set; }

                public Task Handle(BaseCommand messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.UsedBaseRoute = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class BaseCommand : ICommand
        {
        }

        public class DerivedCommand : BaseCommand
        {
        }
    }
}
#pragma warning restore CS0618