namespace NServiceBus.Core.Tests.Audit
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class InvokeAuditPipelineBehaviorTests
    {
        [Test]
        public async Task Should_not_leak_header_modifications_into_main_pipeline()
        {
            var behavior = new InvokeAuditPipelineBehavior("auditAddress");
            var context = new TestableIncomingPhysicalMessageContext();
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(new FakeAuditPipeline(ctx =>
            {
                ctx.Message.Headers.Add("test", bool.TrueString);
                return Task.CompletedTask;
            })));

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.IsFalse(context.MessageHeaders.ContainsKey("test"));
        }

        // Failing
        [Test]
        public async Task Should_not_leak_body_modifications_into_main_pipeline()
        {
            var behavior = new InvokeAuditPipelineBehavior("auditAddress");
            var context = new TestableIncomingPhysicalMessageContext();
            context.UpdateMessage(new byte[] { 1, 2, 3 });
            context.Extensions.Set<IPipelineCache>(new FakePipelineCache(new FakeAuditPipeline(ctx =>
            {
                ctx.Message.Body[0] = 42;
                return Task.CompletedTask;
            })));

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, context.Message.Body);
        }

        class FakePipelineCache : IPipelineCache
        {
            FakeAuditPipeline auditPipeline;

            public FakePipelineCache(FakeAuditPipeline auditPipeline)
            {
                this.auditPipeline = auditPipeline;
            }

            public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext
            {
                if (typeof(TContext) == typeof(IAuditContext))
                {
                    return (IPipeline<TContext>)auditPipeline;
                }

                throw new NotImplementedException();
            }
        }

        class FakeAuditPipeline : IPipeline<IAuditContext>
        {
            Func<IAuditContext, Task> pipelineAction;

            public FakeAuditPipeline(Func<IAuditContext, Task> pipelineAction)
            {
                this.pipelineAction = pipelineAction;
            }

            public Task Invoke(IAuditContext context) => pipelineAction(context);
        }
    }
}