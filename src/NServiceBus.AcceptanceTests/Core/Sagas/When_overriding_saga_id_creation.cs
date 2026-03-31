namespace NServiceBus.AcceptanceTests.Core.Sagas;

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
            .Run();

        Assert.That(context.SagaId, Is.EqualTo(new Guid("1d99288a-418d-9e4d-46e4-d49a27908fc8")));
    }

    public class Context : ScenarioContext
    {
        public Guid? SagaId { get; set; }
    }

    public class EndpointThatHostsASaga : EndpointConfigurationBuilder
    {
        public EndpointThatHostsASaga() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.UsePersistence<AcceptanceTestingPersistence>();
                config.GetSettings().Set<ISagaIdGenerator>(new CustomSagaIdGenerator());
            });

        public class CustomSagaIdGenerator : ISagaIdGenerator
        {
            public Guid Generate(SagaIdGeneratorContext context) => ToGuid($"{context.SagaMetadata.SagaEntityType.FullName}_{context.CorrelationProperty.Name}_{context.CorrelationProperty.Value}");

            static Guid ToGuid(string src)
            {
                var stringbytes = Encoding.UTF8.GetBytes(src);

                var hashedBytes = SHA1.HashData(stringbytes);
                Array.Resize(ref hashedBytes, 16);
                return new Guid(hashedBytes);
            }
        }

        [Saga]
        public class CustomSagaIdSaga(Context testContext) : Saga<CustomSagaIdSaga.CustomSagaIdSagaData>,
            IAmStartedByMessages<StartSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.CustomerId = message.CustomerId;
                testContext.SagaId = Data.Id;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomSagaIdSagaData> mapper) =>
                mapper.MapSaga(s => s.CustomerId)
                    .ToMessage<StartSaga>(m => m.CustomerId);

            public class CustomSagaIdSagaData : ContainSagaData
            {
                public virtual string CustomerId { get; set; }
            }
        }
    }

    public class StartSaga : IMessage
    {
        public string CustomerId { get; set; }
    }
}