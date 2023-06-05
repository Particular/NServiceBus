namespace NServiceBus.AcceptanceTests.Core.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_outbox_enabled_on_send_only : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_make_outbox_and_storage_session_available()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.HasOutboxStorage);
            Assert.True(context.HasSynchronizedStorageSession);
        }

        class Context : ScenarioContext
        {
            public bool HasOutboxStorage { get; set; }
            public bool HasSynchronizedStorageSession { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.GetSettings().Set("Outbox.SkipPrerequisitesCheck", true);
                    c.EnableOutbox();
                    c.EnableFeature<FeatureAccessingOutbox>();
                    c.SendOnly();
                });
            }

            class FeatureAccessingOutbox : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    var testContext = (Context)context.Settings.Get<ScenarioContext>();

                    context.RegisterStartupTask(serviceProvider => new StartupTask(testContext, serviceProvider));
                }
            }

            class StartupTask : FeatureStartupTask
            {
                readonly Context testContext;
                readonly IServiceProvider serviceProvider;

                public StartupTask(Context testContext, IServiceProvider serviceProvider)
                {
                    this.testContext = testContext;
                    this.serviceProvider = serviceProvider;
                }
                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    testContext.HasOutboxStorage = serviceProvider.GetService<IOutboxStorage>() != null;
                    testContext.HasSynchronizedStorageSession = serviceProvider.GetService<ICompletableSynchronizedStorageSession>() != null;

                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    return Task.CompletedTask;
                }
            }
        }
    }
}