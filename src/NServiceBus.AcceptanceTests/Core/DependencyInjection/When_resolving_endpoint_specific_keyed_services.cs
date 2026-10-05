namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_endpoint_specific_keyed_services : NServiceBusAcceptanceTest
{
    // When registering keyed services globally that match endpoint-specific keys, each endpoint should resolve its own
    // global keyed service without having to use FromKeyedServices. Local keyed services using the same logical key
    // should still be isolated per endpoint.
    [Test]
    public async Task Should_be_possible()
    {
        var result = await Scenario.Define<Context>()
            .WithServices(static services =>
            {
                var firstEndpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(FirstEndpointUsingGlobalKeyedService));
                var secondEndpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(SecondEndpointUsingGlobalKeyedService));

                services.AddKeyedSingleton<IMyComponent, FirstEndpointComponent>($"{firstEndpointName}1");
                services.AddKeyedSingleton<IMyComponent, SecondEndpointComponent>($"{secondEndpointName}2");
            })
            .WithEndpoint<FirstEndpointUsingGlobalKeyedService>(b => b
                .Services(static services => services.AddKeyedSingleton<ILocalComponent, FirstLocalComponent>("local"))
                .When((session, _) => session.SendLocal(new SomeMessage())))
            .WithEndpoint<SecondEndpointUsingGlobalKeyedService>(b => b
                .Services(static services => services.AddKeyedSingleton<ILocalComponent, SecondLocalComponent>("local"))
                .When((session, _) => session.SendLocal(new SomeMessage())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FirstComponent, Is.InstanceOf<FirstEndpointComponent>());
            Assert.That(result.FirstLocalComponent, Is.InstanceOf<FirstLocalComponent>());
            Assert.That(result.SecondComponent, Is.InstanceOf<SecondEndpointComponent>());
            Assert.That(result.SecondLocalComponent, Is.InstanceOf<SecondLocalComponent>());
        }
    }

    public class Context : ScenarioContext
    {
        public IMyComponent FirstComponent { get; set; }
        public ILocalComponent FirstLocalComponent { get; set; }
        public IMyComponent SecondComponent { get; set; }
        public ILocalComponent SecondLocalComponent { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(FirstComponent is not null && FirstLocalComponent is not null && SecondComponent is not null && SecondLocalComponent is not null);
    }

    public interface IMyComponent;
    public interface ILocalComponent;

    public class FirstEndpointComponent : IMyComponent;
    public class FirstLocalComponent : ILocalComponent;

    public class SecondEndpointComponent : IMyComponent;
    public class SecondLocalComponent : ILocalComponent;

    public class FirstEndpointUsingGlobalKeyedService : EndpointConfigurationBuilder
    {
        public FirstEndpointUsingGlobalKeyedService() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler(Context testContext, IMyComponent component, [FromKeyedServices("local")] ILocalComponent localComponent) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.FirstComponent = component;
                testContext.FirstLocalComponent = localComponent;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SecondEndpointUsingGlobalKeyedService : EndpointConfigurationBuilder
    {
        public SecondEndpointUsingGlobalKeyedService() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler(Context testContext, IMyComponent component, [FromKeyedServices("local")] ILocalComponent localComponent) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.SecondComponent = component;
                testContext.SecondLocalComponent = localComponent;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : ICommand;
}
