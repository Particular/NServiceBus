namespace NServiceBus.AcceptanceTests.Session
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using Features;
    using NServiceBus;
    using NUnit.Framework;

    public class When_session_floating_abused : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_detected()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFloatingSession>(b => b.When(async session =>
                    {
                        await session.SendLocal(new MyMessage());
                    })
                    .DoNotFailOnErrorMessages())
                .Done(c => c.ExceptionCaught != null)
                .Run();

            Assert.IsTrue(context.ExceptionCaught.Message.Contains("has a longer lifetime than the handler"));
        }

        public class Context : ScenarioContext
        {
            public TaskCompletionSource<bool> Synchronizer = new TaskCompletionSource<bool>();
            public Exception ExceptionCaught { get; set; }
        }

        public class EndpointWithFloatingSession : EndpointConfigurationBuilder
        {
            public EndpointWithFloatingSession()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableFeature<FeatureThatSends>();

                    b.RegisterComponents(c => c.ConfigureComponent<MyDependency>(DependencyLifecycle.SingleInstance));
                    b.RegisterComponents(c => c.ConfigureComponent<MyOtherDependency>(DependencyLifecycle.SingleInstance));

                    b.FloatScopedSession();

                    b.Pipeline.OnReceivePipelineCompleted(e =>
                    {
                        var testContext = (Context)b.GetSettings().Get<ScenarioContext>();
                        testContext.Synchronizer.TrySetResult(true);
                        return Task.FromResult(0);
                    });
                });
            }

            public class FeatureThatSends : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(b => new StartupTaskThatSends(() => b.Build<MyDependency>(), b.Build<Context>()));
                }

                public class StartupTaskThatSends : FeatureStartupTask
                {
                    public StartupTaskThatSends(Func<MyDependency> dependencyFactory, Context testContext)
                    {
                        this.testContext = testContext;
                        this.dependencyFactory = dependencyFactory;
                    }

                    protected override Task OnStart(IMessageSession session)
                    {
                        task = Task.Run(async () =>
                        {
                            await testContext.Synchronizer.Task;
                            var dependency = dependencyFactory();
                            try
                            {
                                await dependency.Do();
                            }
                            catch (Exception e)
                            {
                                testContext.ExceptionCaught = e;
                            }
                        });
                        return Task.CompletedTask;
                    }

                    protected override Task OnStop(IMessageSession session)
                    {
                        return task;
                    }

                    Func<MyDependency> dependencyFactory;
                    Context testContext;
                    Task task;
                }
            }

            public class MyHandler : IHandleMessages<MyMessage>
            {
                public MyHandler(MyDependency dependency)
                {
                    this.dependency = dependency;
                }

                public Context TestContext { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return Task.CompletedTask;
                }

                // ReSharper disable once NotAccessedField.Local
                MyDependency dependency;
            }

            public class MyOtherDependency
            {
                public MyOtherDependency(IScopedMessageSession session)
                {
                    this.session = session;
                }

                public Task Do()
                {
                    return session.SendLocal(new PotentiallyEscapingMessage());
                }

                IScopedMessageSession session;
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