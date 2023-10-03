﻿namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
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
            SpyContainer spyContainer = null;
            await Scenario.Define<Context>()
                .WithEndpoint<StartedEndpoint>(b =>
                {
                    b.ToCreateInstance(
                        endpointConfiguration => Task.FromResult(EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection)),
                        startableEndpoint =>
                        {
                            spyContainer = new SpyContainer(serviceCollection);
                            return startableEndpoint.Start(spyContainer);
                        });
                    b.When(e => e.SendLocal(new SomeMessage()));
                })
                .Done(c => c.GotTheMessage)
                .Run();

            var builder = new StringBuilder();
            var coreComponents = spyContainer.RegisteredServices.Values
                .OrderBy(c => c.Type.FullName)
                .ToList();

            var privateComponents = coreComponents.Where(c => !c.Type.IsPublic);
            var publicComponents = coreComponents.Where(c => c.Type.IsPublic);

            builder.AppendLine("----------- Public registrations used by Core -----------");

            foreach (var component in publicComponents.Where(c => c.WasResolved))
            {
                builder.AppendLine(component.ToString());
            }

            builder.AppendLine("----------- Public registrations not used by Core -----------");

            foreach (var component in publicComponents.Where(c => !c.WasResolved))
            {
                builder.AppendLine(component.ToString());
            }

            builder.AppendLine("----------- Private registrations used by Core-----------");

            foreach (var component in privateComponents.Where(c => c.WasResolved))
            {
                builder.AppendLine(component.ToString());
            }

            builder.AppendLine("----------- Private registrations not used by Core -----------");

            foreach (var component in privateComponents.Where(c => !c.WasResolved))
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
                EndpointSetup<DefaultServer>();
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

                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        public class SomeMessage : IMessage
        {
        }

        class SpyContainer : IServiceProvider, IServiceScopeFactory
        {
            public Dictionary<Type, RegisteredService> RegisteredServices { get; } = new Dictionary<Type, RegisteredService>();

            IServiceProvider ServiceProvider { get; }

            public SpyContainer(IServiceCollection serviceCollection)
            {
                foreach (var serviceDescriptor in serviceCollection
                    .Where(sd => sd.ServiceType.Assembly == typeof(IEndpointInstance).Assembly))
                {
                    RegisteredServices[serviceDescriptor.ServiceType] = new RegisteredService
                    {
                        Type = serviceDescriptor.ServiceType,
                        Lifecycle = serviceDescriptor.Lifetime
                    };
                }

                ServiceProvider = serviceCollection.BuildServiceProvider();
            }

            public object GetService(Type serviceType)
            {
                if (RegisteredServices.TryGetValue(serviceType, out var registeredService))
                {
                    registeredService.WasResolved = true;
                }

                return serviceType == typeof(IServiceScopeFactory) ? this : ServiceProvider.GetService(serviceType);
            }

            public IServiceScope CreateScope()
            {
                var scope = ServiceProvider.CreateScope();
                return new SpyScope(scope, RegisteredServices);
            }

            class SpyScope : IServiceScope, IServiceProvider
            {
                readonly IServiceScope decorated;
                readonly Dictionary<Type, RegisteredService> registeredServices;

                public SpyScope(IServiceScope decorated, Dictionary<Type, RegisteredService> registeredServices)
                {
                    this.registeredServices = registeredServices;
                    this.decorated = decorated;
                }

                public IServiceProvider ServiceProvider => this;
                public object GetService(Type serviceType)
                {
                    if (registeredServices.TryGetValue(serviceType, out var registeredService))
                    {
                        registeredService.WasResolved = true;
                    }

                    return decorated.ServiceProvider.GetService(serviceType);
                }

                public void Dispose() => decorated.Dispose();
            }

            public class RegisteredService
            {
                public Type Type { get; set; }
                public ServiceLifetime Lifecycle { get; set; }
                public bool WasResolved { get; set; }

                public override string ToString() => $"{Type.FullName} - {Lifecycle}";
            }
        }
    }
}