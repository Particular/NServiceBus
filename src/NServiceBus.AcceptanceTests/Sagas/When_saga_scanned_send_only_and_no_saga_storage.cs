namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_saga_scanned_send_only_and_no_saga_storage : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_throw()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<SendOnlyEndpointWithSaga>()
                    .Done(c => c.EndpointsStarted)
                    .Run();
            });

        }

        class SendOnlyEndpointWithSaga : EndpointConfigurationBuilder
        {
            public SendOnlyEndpointWithSaga()
            {
                EndpointSetup<ServerWithNoDefaultPersistenceDefinitions>(c =>
                {
                    c.UsePersistence<AcceptanceTestingPersistence, StorageType.Subscriptions>();

                    c.SendOnly();
                });
            }
        }

        public class SagaInSendOnlyEndpoint : Saga<SagaInSendOnlyEndpoint.SagaInSendOnlyEndpointSagaData>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaInSendOnlyEndpointSagaData> mapper)
            {
            }

            public class SagaInSendOnlyEndpointSagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }

        public class Context : ScenarioContext
        {
        }
    }
}