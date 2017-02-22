namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_overriding_saga_id_creation : NServiceBusAcceptanceTest
    {
        [Test,Ignore("Invalid test since the dev persister requires the ID to be a guid based of the corr prop?")]
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

            Assert.AreEqual(new Guid("5ebef5b7-815e-653c-2ee7-37ed83d7d7b5"), context.SagaId);
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
                    config.EnableFeature<TimeoutManager>();
                    config.RegisterComponents(c => c.RegisterSingleton<ISagaIdGenerator>(new CustomSagaIdGenerator()));
                });
            }

            public class MySaga : Saga<MySaga.MySagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.CustomerId = message.CustomerId;
                    TestContext.SagaId = Data.Id;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.CustomerId).ToSaga(s => s.CustomerId);
                }

                public class MySagaData : ContainSagaData
                {
                    public virtual string CustomerId { get; set; }
                }

                public class TimeHasPassed
                {
                }
            }
        }

        public class StartSaga : IMessage
        {
            public string CustomerId { get; set; }
        }

        class CustomSagaIdGenerator : ISagaIdGenerator
        {
            public Guid Generate(string propertyName, object propertyValue, SagaMetadata metadata, ContextBag context)
            {
                return ToGuid($"{metadata.SagaEntityType.FullName}_{propertyName}_{propertyValue}");
            }

            static Guid ToGuid(string src)
            {
                var stringbytes = Encoding.UTF8.GetBytes(src);
                using (var provider = new SHA1CryptoServiceProvider())
                {
                    var hashedBytes = provider.ComputeHash(stringbytes);
                    Array.Resize(ref hashedBytes, 16);
                    return new Guid(hashedBytes);
                }
            }
        }
    }
}