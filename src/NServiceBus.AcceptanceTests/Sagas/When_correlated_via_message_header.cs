namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class When_correlated_via_message_header : NServiceBusAcceptanceTest
    {
        const string CorrelationHeader = nameof(CorrelationHeader);

        [Test]
        public async Task Should_succeed_when_header_is_present()
        {
            var scenario = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaWithHeaderMapping>(c => c
                    .When(async session =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.SetHeader(CorrelationHeader, "5");
                        sendOptions.RouteToThisEndpoint();

                        await session.Send(new StartSaga(), sendOptions);
                    }))
                .Done(ctx => ctx.Done)
                .Run();

            Assert.That(scenario.CorrelationId, Is.EqualTo(5));
        }

        [Test]
        public void Should_throw_when_header_is_missing()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithSagaWithHeaderMapping>(c => c
                        .When(async session =>
                        {
                            var sendOptions = new SendOptions();
                            sendOptions.RouteToThisEndpoint();

                            await session.Send(new StartSaga(), sendOptions);
                        }
                    ))
                    .Done(ctx => ctx.Done)
                    .Run()
            );

            Assert.That(exception.FailedMessage.Exception.Message, Does.Contain("missing a header used for correlation"));
            Assert.That(exception.FailedMessage.Exception.Message, Does.Contain(CorrelationHeader));
        }

        [Test]
        public void Should_throw_when_header_cannot_be_cast_to_correlation_property_type()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithSagaWithHeaderMapping>(c => c
                        .When(async session =>
                        {
                            var sendOptions = new SendOptions();
                            sendOptions.SetHeader(CorrelationHeader, "FIVE");
                            sendOptions.RouteToThisEndpoint();

                            await session.Send(new StartSaga(), sendOptions);
                        }
                    ))
                    .Done(ctx => ctx.Done)
                    .Run()
            );

            Assert.That(exception.FailedMessage.Exception.Message, Does.Contain("cannot be cast to correlation property type"));
            Assert.That(exception.FailedMessage.Exception.Message, Does.Contain(CorrelationHeader));
            Assert.That(exception.FailedMessage.Exception.Message, Does.Contain("FIVE"));
            Assert.That(exception.FailedMessage.Exception.Message, Does.Contain(typeof(int).ToString()));
        }

        public class StartSaga : ICommand { }

        public class SagaDataWithHeaderMapping : ContainSagaData
        {
            public virtual int CorrelationId { get; set; }
        }

        public class EndpointWithSagaWithHeaderMapping : EndpointConfigurationBuilder
        {
            public EndpointWithSagaWithHeaderMapping()
            {
                EndpointSetup<DefaultServer>(cfg => 
                    cfg.Pipeline.Register(typeof(EndTestOnException), "Ends test if an exception occurs"));
            }

            public class SagaWithHeaderMapping : Saga<SagaDataWithHeaderMapping>, IAmStartedByMessages<StartSaga>
            {
                Context scenario;

                public SagaWithHeaderMapping(Context scenario)
                {
                    this.scenario = scenario;
                }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    scenario.CorrelationId = Data.CorrelationId;
                    scenario.Done = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithHeaderMapping> mapper)
                {
                    mapper.ConfigureHeaderMapping<StartSaga>(CorrelationHeader)
                        .ToSaga(saga => saga.CorrelationId);
                }
            }

            class EndTestOnException : Behavior<IIncomingLogicalMessageContext>
            {
                Context scenario;

                public EndTestOnException(Context scenario)
                {
                    this.scenario = scenario;
                }

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    try
                    {
                        await next();
                    }
                    catch
                    {
                        scenario.Done = true;
                        throw;
                    }
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public int CorrelationId { get; set; }
        }
    }
}