namespace NServiceBus.AcceptanceTests.Core.Conventions;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.AcceptanceTesting.Customization;
using NUnit.Framework;

public class When_scanning_an_assembly_containing_a_ref_struct_and_sagas_enabled : NServiceBusAcceptanceTest
{
    [Test]
    public void It_should_not_throw_an_exception()
        => Assert.DoesNotThrowAsync(
            () => Scenario.Define<ScenarioContext>()
                          .WithEndpoint<EndpointWithASaga>()
                          .Done(c => c.EndpointsStarted)
                          .Run()
    );

    // HINT: This will get picked up by the AssemblyRouteSource created by the routing call below
    // Even though it is not a message type, it is still checked by passing it to conventions.
    // The conventions added by Sagas were throwing an exception when passed a ref struct.
    // See https://github.com/Particular/NServiceBus/issues/7179 for details.
    ref struct RefStruct { }

    public class EndpointWithASaga : EndpointConfigurationBuilder
    {
        public EndpointWithASaga() => EndpointSetup<DefaultServer>(cfg => cfg
            .ConfigureRouting()
            .RouteToEndpoint(
                typeof(RefStruct).Assembly,
                Conventions.EndpointNamingConvention(typeof(EndpointWithASaga))
            )
        );

        [Saga]
        public class RealSagaToSetUpConventions : Saga<RealSagaToSetUpConventions.RealSagaToSetUpConventionsSagaData>, IAmStartedByMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RealSagaToSetUpConventionsSagaData> mapper)
                => mapper.MapSaga(saga => saga.BusinessId).ToMessage<SomeMessage>(msg => msg.BusinessId);

            public class RealSagaToSetUpConventionsSagaData : ContainSagaData
            {
                public virtual Guid BusinessId { get; set; }
            }
        }
    }

    public class SomeMessage : IMessage
    {
        public Guid BusinessId { get; set; }
    }
}
