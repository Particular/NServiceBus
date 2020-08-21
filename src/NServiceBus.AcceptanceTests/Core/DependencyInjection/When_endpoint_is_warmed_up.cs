namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Particular.Approvals;

    public class When_endpoint_is_warmed_up : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Make_sure_things_are_in_DI()
        {
            var serviceCollection = new ServiceCollection();
            SpyConainer spyContainer = null;
            await Scenario.Define<Context>()
                .WithEndpoint<StartedEndpoint>(b =>
                {
                    b.ToCreateInstance(
                        endpointConfiguration => Task.FromResult(EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection)),
                        startableEndpoint =>
                        {
                            spyContainer = new SpyConainer(serviceCollection);
                            return startableEndpoint.Start(spyContainer);
                        });
                    b.When(e => e.SendLocal(new SomeMessage()));
                })
                .Done(c => c.GotTheMessage)
                .Run();

            var builder = new StringBuilder();
            var coreComponents = spyContainer.RegisteredComponents.Values
                .OrderBy(c => c.Type.FullName)
                .ToList();

            builder.AppendLine("----------- Used registrations (Find ways to stop accessing them)-----------");

            foreach (var component in coreComponents.Where(c => c.WasResolved))
            {
                builder.AppendLine(component.ToString());
            }

            builder.AppendLine("----------- Registrations not used by the core, can be removed in next major if downstreams have been confirmed to not use it -----------");

            foreach (var component in coreComponents.Where(c => !c.WasResolved))
            {
                builder.AppendLine(component.ToString());
            }

            Approver.Verify(builder.ToString());
        }

        class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<ExternallyManagedContainerServer>();
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public SomeMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.GotTheMessage = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class SomeMessage : IMessage
        {
        }

        class SpyConainer : IServiceProvider
        {
            public Dictionary<Type, RegisteredService> RegisteredComponents { get; } = new Dictionary<Type, RegisteredService>();

            IServiceProvider ServiceProvider { get; }

            public SpyConainer(IServiceCollection serviceCollection)
            {
                foreach (var serviceDescriptor in serviceCollection
                    .Where(sd => sd.ServiceType.Assembly == typeof(IMessage).Assembly))
                {
                    RegisteredComponents[serviceDescriptor.ServiceType] = new RegisteredService
                    {
                        Type = serviceDescriptor.ServiceType,
                        Lifecycle = serviceDescriptor.Lifetime
                    };
                }

                ServiceProvider = serviceCollection.BuildServiceProvider();
            }

            public object GetService(Type serviceType)
            {
                if (RegisteredComponents.TryGetValue(serviceType, out var registeredService))
                {
                    registeredService.WasResolved = true;
                }

                return ServiceProvider.GetService(serviceType);
            }

            public class RegisteredService
            {
                public Type Type { get; set; }
                public ServiceLifetime Lifecycle { get; set; }
                public bool WasResolved { get; set; }

                public override string ToString()
                {
                    return $"{Type.FullName} - {Lifecycle}";
                }
            }
        }
    }
}