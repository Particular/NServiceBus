namespace NServiceBus.AcceptanceTests.SelfVerification
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_custom_components : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_properly_start_and_stop_them()
        {
            var ctx = await Scenario.Define<Context>()
                .WithCustomComponent("MyComponent", (context, token) =>
                {
                    context.Starting = true;
                    return Task.FromResult(0);
                }, context =>
                {
                    context.Stopped = true;
                    return Task.FromResult(0);
                }, (context, token) =>
                {
                    context.Started = true;
                    return Task.FromResult(0);
                })
                .Done(c => c.Starting)
                .Run();

            Assert.IsTrue(ctx.Starting);
            Assert.IsTrue(ctx.Started);
            Assert.IsTrue(ctx.Stopped);
        }

        class Context : ScenarioContext
        {
            public bool Starting { get; set; }
            public bool Started { get; set; }
            public bool Stopped { get; set; }
        }
    }
}