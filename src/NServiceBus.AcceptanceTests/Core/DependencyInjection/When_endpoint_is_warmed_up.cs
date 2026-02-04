namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

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
        IServiceCollection serviceCollection = null;
        SpyContainer spyContainer = null;

        await Scenario.Define<Context>()
            .WithEndpoint<StartedEndpoint>(b =>
                b.ToCreateInstance((services, configuration) =>
                {
                    serviceCollection = services;
                    return EndpointWithExternallyManagedContainer.Create(configuration, services);
                }, (startableEndpoint, provider, ct) =>
                {
                    spyContainer = new SpyContainer(serviceCollection, provider);
                    return startableEndpoint.Start(spyContainer, ct);
                })
                .When(session => session.SendLocal(new SomeMessage())))
            .Run();

        var builder = new StringBuilder();
        var coreComponents = spyContainer.RegisteredServices.Values
            .OrderBy(c => c.Type.FullName)
            .ToList();

        var privateComponents = coreComponents.Where(c => !c.Type.IsPublic).ToArray();
        var publicComponents = coreComponents.Where(c => c.Type.IsPublic).ToArray();

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

    public class Context : ScenarioContext;

    public class StartedEndpoint : EndpointConfigurationBuilder
    {
        public StartedEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage;

    class SpyContainer : IServiceProvider, ISupportRequiredService, IServiceScopeFactory
    {
        public Dictionary<Type, RegisteredService> RegisteredServices { get; } = [];

        IServiceProvider ServiceProvider { get; }

        public SpyContainer(IServiceCollection serviceCollection, IServiceProvider provider)
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

            ServiceProvider = provider;
        }

        public object GetService(Type serviceType)
        {
            if (RegisteredServices.TryGetValue(serviceType, out var registeredService))
            {
                registeredService.WasResolved = true;
            }

            return serviceType == typeof(IServiceScopeFactory) ? this : ServiceProvider.GetService(serviceType);
        }

        public object GetRequiredService(Type serviceType)
        {
            if (RegisteredServices.TryGetValue(serviceType, out var registeredService))
            {
                registeredService.WasResolved = true;
            }

            return serviceType == typeof(IServiceScopeFactory) ? this : ServiceProvider.GetRequiredService(serviceType);
        }

        public IServiceScope CreateScope()
        {
            var scope = ServiceProvider.CreateAsyncScope();
            return new SpyScope(scope, RegisteredServices);
        }

        class SpyScope(AsyncServiceScope decorated, Dictionary<Type, RegisteredService> registeredServices)
            : IServiceScope, ISupportRequiredService, IServiceProvider, IAsyncDisposable
        {
            public IServiceProvider ServiceProvider => this;
            public object GetService(Type serviceType)
            {
                if (registeredServices.TryGetValue(serviceType, out var registeredService))
                {
                    registeredService.WasResolved = true;
                }

                return decorated.ServiceProvider.GetService(serviceType);
            }

            public object GetRequiredService(Type serviceType)
            {
                if (registeredServices.TryGetValue(serviceType, out var registeredService))
                {
                    registeredService.WasResolved = true;
                }

                return decorated.ServiceProvider.GetRequiredService(serviceType);
            }

            public void Dispose() => decorated.Dispose();

            public async ValueTask DisposeAsync() => await decorated.DisposeAsync();
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