namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_message_session_for_multi_hosted_endpoints : NServiceBusAcceptanceTest
{
    // When multiple endpoints share a host, TryAddSingleton<IMessageSession> is executed for each
    // endpoint via the KeyedServiceCollectionAdapter, which automatically converts the plain
    // registration into a keyed one scoped to the endpoint's identifier. This test verifies that
    // each endpoint gets its own independent IMessageSession that is resolvable by key from the
    // global service provider and is functional for sending messages.
    [Test]
    public async Task Should_resolve_distinct_session_per_endpoint()
    {
        var result = await Scenario.Define<Context>()
            .WithEndpoint<FirstEndpoint>()
            .WithEndpoint<SecondEndpoint>()
            .WithServiceResolve(static async (provider, context, ct) =>
            {
                var firstName = Conventions.EndpointNamingConvention(typeof(FirstEndpoint));
                var secondName = Conventions.EndpointNamingConvention(typeof(SecondEndpoint));

                var firstSession = provider.GetRequiredKeyedService<IMessageSession>($"{firstName}0");
                var secondSession = provider.GetRequiredKeyedService<IMessageSession>($"{secondName}1");

                context.SessionsAreDistinct = !ReferenceEquals(firstSession, secondSession);

                await firstSession.SendLocal(new SomeMessage(), ct);
                await secondSession.SendLocal(new SomeMessage(), ct);
            })
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SessionsAreDistinct, Is.True, "Each endpoint should have its own IMessageSession instance");
            Assert.That(result.FirstEndpointReceived, Is.True, "FirstEndpoint should have received the message sent via its session");
            Assert.That(result.SecondEndpointReceived, Is.True, "SecondEndpoint should have received the message sent via its session");
        }
    }

    public class Context : ScenarioContext
    {
        public bool SessionsAreDistinct { get; set; }
        public bool FirstEndpointReceived { get; set; }
        public bool SecondEndpointReceived { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(FirstEndpointReceived && SecondEndpointReceived);
    }

    public class FirstEndpoint : EndpointConfigurationBuilder
    {
        public FirstEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler(Context context) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext ctx)
            {
                context.FirstEndpointReceived = true;
                context.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SecondEndpoint : EndpointConfigurationBuilder
    {
        public SecondEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler(Context context) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext ctx)
            {
                context.SecondEndpointReceived = true;
                context.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : ICommand;
}