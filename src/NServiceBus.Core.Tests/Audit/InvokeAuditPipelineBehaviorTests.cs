namespace NServiceBus.Core.Tests.Audit;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class InvokeAuditPipelineBehaviorTests
{
    [Test]
    public async Task Should_invoke_next_before_forking_audit_pipeline()
    {
        var behavior = new InvokeAuditPipelineBehavior("audit", TimeSpan.FromMinutes(1));
        var context = CreateContext(new RecordingAuditPipeline());
        var nextWasCalled = false;

        await behavior.Invoke(context, _ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextWasCalled, Is.True);
    }

    [Test]
    public async Task Should_create_audit_message_with_copied_headers()
    {
        var auditPipeline = new RecordingAuditPipeline();
        var behavior = new InvokeAuditPipelineBehavior("audit", TimeSpan.FromMinutes(1));
        var context = CreateContext(auditPipeline);
        context.Message.Headers["Custom"] = "value";

        await behavior.Invoke(context, _ => Task.CompletedTask);

        var audited = auditPipeline.LastContext.Message;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(audited.Headers, Is.EquivalentTo(context.Message.Headers));
            Assert.That(audited.Headers, Is.Not.SameAs(context.Message.Headers));
        }
    }

    [Test]
    public async Task Should_use_incoming_message_id_and_body_for_audit_message()
    {
        var auditPipeline = new RecordingAuditPipeline();
        var behavior = new InvokeAuditPipelineBehavior("audit", TimeSpan.FromMinutes(1));
        var context = CreateContext(auditPipeline);

        var body = new byte[] { 1, 2, 3, 4 };
        context.Message = new IncomingMessage("incoming-id", [], body);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        var audited = auditPipeline.LastContext.Message;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(audited.MessageId, Is.EqualTo("incoming-id"));
            Assert.That(audited.Body.ToArray(), Is.EqualTo(body));
        }
    }

    [Test]
    public async Task Should_pass_audit_address_and_ttbr_to_audit_context()
    {
        var auditPipeline = new RecordingAuditPipeline();
        var timeToBeReceived = TimeSpan.FromMinutes(5);
        var behavior = new InvokeAuditPipelineBehavior("configured-audit-address", timeToBeReceived);
        var context = CreateContext(auditPipeline);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(auditPipeline.LastContext.AuditAddress, Is.EqualTo("configured-audit-address"));
            Assert.That(auditPipeline.LastContext.TimeToBeReceived, Is.EqualTo(timeToBeReceived));
        }
    }

    [Test]
    public void Should_not_fork_audit_pipeline_when_next_throws()
    {
        var auditPipeline = new RecordingAuditPipeline();
        var behavior = new InvokeAuditPipelineBehavior("audit", TimeSpan.FromMinutes(1));
        var context = CreateContext(auditPipeline);

        var expected = new Exception("next failed");

        var thrown = Assert.ThrowsAsync<Exception>(() => behavior.Invoke(context, _ => Task.FromException(expected)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(thrown, Is.SameAs(expected));
            Assert.That(auditPipeline.InvocationCount, Is.EqualTo(0));
        }
    }

    static TestableIncomingPhysicalMessageContext CreateContext(RecordingAuditPipeline pipeline)
    {
        var context = new TestableIncomingPhysicalMessageContext();
        context.Extensions.Set<IPipelineCache>(new FakePipelineCache(pipeline));
        return context;
    }

    sealed class RecordingAuditPipeline : IPipeline<IAuditContext>
    {
        public int InvocationCount { get; private set; }
        public IAuditContext LastContext { get; private set; }

        public Task Invoke(IAuditContext context)
        {
            InvocationCount++;
            LastContext = context;
            return Task.CompletedTask;
        }
    }

    sealed class FakePipelineCache(IPipeline<IAuditContext> pipeline) : IPipelineCache
    {
        public IPipeline<TContext> Pipeline<TContext>()
            where TContext : IBehaviorContext =>
            (IPipeline<TContext>)pipeline;
    }
}
