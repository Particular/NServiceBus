﻿namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus.Sagas;
using NUnit.Framework;

public class When_overriding_saga_id_creation : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_generate_saga_id_accordingly()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointThatHostsASaga>(
                b => b.When(session => session.SendLocal(new StartSaga
                {
                    CustomerId = "42",
                })))
            .Done(c => c.SagaId.HasValue)
            .Run();

        Assert.That(context.SagaId, Is.EqualTo(new Guid("1d99288a-418d-9e4d-46e4-d49a27908fc8")));
    }

    public class Context : ScenarioContext
    {
        public Guid? SagaId { get; set; }
    }

    public class EndpointThatHostsASaga : EndpointConfigurationBuilder
    {
        public EndpointThatHostsASaga()
        {
            EndpointSetup<DefaultServer>(config =>
            {
                config.UsePersistence<AcceptanceTestingPersistence>();
                config.GetSettings().Set<ISagaIdGenerator>(new CustomSagaIdGenerator());
            });
        }

        class CustomSagaIdGenerator : ISagaIdGenerator
        {
            public Guid Generate(SagaIdGeneratorContext context)
            {
                return ToGuid($"{context.SagaMetadata.SagaEntityType.FullName}_{context.CorrelationProperty.Name}_{context.CorrelationProperty.Value}");
            }

            static Guid ToGuid(string src)
            {
                var stringbytes = Encoding.UTF8.GetBytes(src);

                var hashedBytes = SHA1.HashData(stringbytes);
                Array.Resize(ref hashedBytes, 16);
                return new Guid(hashedBytes);
            }
        }

        public class CustomSagaIdSaga : Saga<CustomSagaIdSaga.CustomSagaIdSagaData>,
            IAmStartedByMessages<StartSaga>
        {
            public CustomSagaIdSaga(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.CustomerId = message.CustomerId;
                testContext.SagaId = Data.Id;

                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomSagaIdSagaData> mapper)
            {
                mapper.ConfigureMapping<StartSaga>(m => m.CustomerId).ToSaga(s => s.CustomerId);
            }

            public class CustomSagaIdSagaData : ContainSagaData
            {
                public virtual string CustomerId { get; set; }
            }

            Context testContext;
        }
    }

    public class StartSaga : IMessage
    {
        public string CustomerId { get; set; }
    }
}