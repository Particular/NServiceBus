namespace NServiceBus.AcceptanceTests.Core.Conventions;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_a_ref_struct_gets_checked_by_conventions_in_an_endpoint_with_a_Saga : NServiceBusAcceptanceTest
{
    // NOTE: AssemblyRouteSource and NamespaceRouteSource do not filter out ref structs encountered in a message assembly.
    // The conventions added by the Sagas feature were throwing an exception when passed a ref struct.
    // See https://github.com/Particular/NServiceBus/issues/7179 for details.
    // This test simulates what the RouteSource objects do so that a separate message assembly is not needed for the test.
    [Test]
    public void It_should_not_throw_an_exception()
        => Assert.DoesNotThrowAsync(
            () => Scenario.Define<Context>()
                          .WithEndpoint<EndpointWithASaga>(
                            // Capture conventions
                            b => b.CustomConfig((endpoint, ctx) => ctx.Conventions = endpoint.Conventions().Conventions)
                          )
                          .Done(ctx => !ctx.Conventions.IsMessageType(typeof(RefStruct)))
                          .Run()
    );

    class Context : ScenarioContext
    {
        public NServiceBus.Conventions Conventions { get; set; }
    }

    class SomeMessage : IMessage
    {
        public Guid BusinessId { get; set; }
    }

    public ref struct RefStruct { }

    class EndpointWithASaga : EndpointConfigurationBuilder
    {
        public EndpointWithASaga() => EndpointSetup<DefaultServer>();

        class MySagaData : ContainSagaData
        {
            public Guid BusinessId { get; set; }
        }

        class MySaga : Saga<MySagaData>, IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
                => mapper.MapSaga(saga => saga.BusinessId).ToMessage<SomeMessage>(msg => msg.BusinessId);
        }
    }
}
