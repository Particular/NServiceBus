namespace NServiceBus.AcceptanceTests.Core.SelfVerification
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_custom_components : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_properly_start_and_stop_them()
        {
            var ctx = await Scenario.Define<Context>()
                .WithComponent(new CustomComponentBehavior())
                .Done(c => c.Starting)
                .Run();

            Assert.IsTrue(ctx.Starting);
            Assert.IsTrue(ctx.ComponentsStarted);
            Assert.IsTrue(ctx.Stopped);
        }

        class Context : ScenarioContext
        {
            public bool Starting { get; set; }
            public bool ComponentsStarted { get; set; }
            public bool Stopped { get; set; }
        }

        class CustomComponentBehavior : IComponentBehavior
        {
            public Task<ComponentRunner> CreateRunner(RunDescriptor run)
            {
                return Task.FromResult<ComponentRunner>(new Runner((Context)run.ScenarioContext));
            }

            class Runner : ComponentRunner
            {
                Context context;

                public Runner(Context context)
                {
                    this.context = context;
                }

                public override string Name => "MyComponent";

                public override Task Start(CancellationToken token)
                {
                    context.Starting = true;
                    return base.Start(token);
                }

                public override Task ComponentsStarted(CancellationToken token)
                {
                    context.ComponentsStarted = true;
                    return base.ComponentsStarted(token);
                }

                public override Task Stop()
                {
                    context.Stopped = true;
                    return base.Stop();
                }
            }
        }
    }
}