namespace NServiceBus.AcceptanceTests.Session
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_session_floating_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_float_session_properly_scoped()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFloatingSession>(b => b.When(async session =>
                    {
                        await session.SendLocal(new MyMessage());
                        await session.SendLocal(new MyMessage());
                    })
                    .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any() && c.NumberOfMyMessagesReceived == 2 && c.NumberOfEscapingMessagesReceived == 1)
                .Run();

            Assert.AreEqual(1, context.NumberOfEscapingMessagesReceived, "The message sent from MyDependency when injected into feature startup task must escape.");
        }

        public class Context : ScenarioContext
        {
            public long NumberOfMyMessagesReceived => Interlocked.Read(ref numberOfMyMessagesReceived);
            public long NumberOfEscapingMessagesReceived => Interlocked.Read(ref numberOfEscapingMessagesReceived);

            public void MyMessageReceived()
            {
                Interlocked.Increment(ref numberOfMyMessagesReceived);
            }

            public void EscapingMessageReceived()
            {
                Interlocked.Increment(ref numberOfEscapingMessagesReceived);
            }

            long numberOfMyMessagesReceived;
            long numberOfEscapingMessagesReceived;
        }

        public class EndpointWithFloatingSession : EndpointConfigurationBuilder
        {
            public EndpointWithFloatingSession()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableFeature<FeatureThatSends>();

                    b.RegisterComponents(c => c.ConfigureComponent<MyDependency>(DependencyLifecycle.InstancePerCall));
                    b.RegisterComponents(c => c.ConfigureComponent<MyOtherDependency>(DependencyLifecycle.InstancePerCall));
                    b.RegisterComponents(c => c.ConfigureComponent<FeatureThatSends.StartupTaskThatSends>(DependencyLifecycle.InstancePerCall));

                    b.FloatScopedSession();
                });
            }

            public class FeatureThatSends : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(b => b.Build<StartupTaskThatSends>());
                }

                public class StartupTaskThatSends : FeatureStartupTask
                {
                    public StartupTaskThatSends(MyDependency dependency)
                    {
                        this.dependency = dependency;
                    }

                    protected override Task OnStart(IMessageSession session)
                    {
                        return dependency.Do();
                    }

                    protected override Task OnStop(IMessageSession session)
                    {
                        return Task.CompletedTask;
                    }

                    MyDependency dependency;
                }
            }

            public class MyHandler : IHandleMessages<MyMessage>
            {
                public MyHandler(IMessageSessionScoped session, Context testContext, MyDependency dependency)
                {
                    this.testContext = testContext;
                    this.session = session;
                    this.dependency = dependency;
                }

                public Context TestContext { get; set; }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    await session.SendLocal(new PotentiallyEscapingMessage());
                    await dependency.Do();
                    testContext.MyMessageReceived();

                    throw new SimulatedException();
                }

                Context testContext;
                IMessageSessionScoped session;
                MyDependency dependency;
            }

            public class MyEscapingHandler : IHandleMessages<PotentiallyEscapingMessage>
            {
                public MyEscapingHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(PotentiallyEscapingMessage message, IMessageHandlerContext context)
                {
                    testContext.EscapingMessageReceived();
                    return Task.CompletedTask;
                }

                Context testContext;
            }

            public class MyOtherDependency
            {
                public MyOtherDependency(IMessageSessionScoped session)
                {
                    this.session = session;
                }

                public Task Do()
                {
                    return session.SendLocal(new PotentiallyEscapingMessage());
                }

                IMessageSessionScoped session;
            }

            public class MyDependency
            {
                public MyDependency(MyOtherDependency dependency)
                {
                    this.dependency = dependency;
                }

                public Task Do()
                {
                    return dependency.Do();
                }

                MyOtherDependency dependency;
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class PotentiallyEscapingMessage : IMessage
        {
        }
    }
}