namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_messages_are_batched_outside_handler_without_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>(c => { c.RunId = Guid.NewGuid(); })
                .WithEndpoint<EndpointWithUnitOfWorkSupport>(b =>
                {
                    b.When((session, c) =>
                    {
                        return Task.CompletedTask;
                    });
                })
                .Done(c => c.Done)
                .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid RunId { get; set; }
            public bool Done { get; set; }
        }

        public class EndpointWithUnitOfWorkSupport : EndpointConfigurationBuilder
        {
            public EndpointWithUnitOfWorkSupport()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                 {
                     config.EnableFeature<UnitOfWorkSessionFeature>();
                     config.EnableFeature<AFeatureBecauseWeHaveNoDIElseWhere>();
                 });
            }

            class AFeatureBecauseWeHaveNoDIElseWhere : Feature
            {
                public AFeatureBecauseWeHaveNoDIElseWhere()
                {
                    DependsOn<UnitOfWorkSessionFeature>();
                }
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Services.AddSingleton<StartupTask>();

                    context.RegisterStartupTask(p => p.GetService<StartupTask>());
                }

                class StartupTask : FeatureStartupTask
                {
                    readonly IUnitOfWorkMessageSessionFactory sessionFactory;

                    public StartupTask(IUnitOfWorkMessageSessionFactory sessionFactory)
                    {
                        this.sessionFactory = sessionFactory;
                    }
                    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        _ = FireAndForgetWithSession(cancellationToken);
                        return Task.CompletedTask;
                    }

                    async Task FireAndForgetWithSession(CancellationToken cancellationToken)
                    {
                        using var unitOfWorkSession = await sessionFactory.OpenSession(cancellationToken: cancellationToken);
                        await unitOfWorkSession.SendLocal(new MessageToBeAudited(), cancellationToken);
                        await unitOfWorkSession.SendLocal(new MessageToBeAudited(), cancellationToken);
                        await unitOfWorkSession.SendLocal(new MessageToBeAudited(), cancellationToken);
                        await unitOfWorkSession.Commit(cancellationToken);
                    }

                    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                }
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }


        public class MessageToBeAudited : IMessage
        {
            public Guid RunId { get; set; }
        }
    }
}