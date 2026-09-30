namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Audit;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NUnit.Framework;
using Testing;

/// <summary>
/// Messages forwarded from the incoming side (recoverability, audit, ForwardCurrentMessageTo) enter the pipeline at
/// the routing stage and never pass through the outgoing pipeline. They must keep the trace headers of the message
/// as it was received and must not be re-parented to whatever activity happens to be current at that time.
/// </summary>
[TestFixture]
public class TraceContextPreservationTests
{
    const string OriginalTraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

    [Test]
    public async Task Should_preserve_trace_headers_when_moving_to_error_queue()
    {
        var recoverabilityContext = CreateRecoverabilityContext();
        var routingContext = new MoveToError("error-queue")
            .GetRoutingContexts(recoverabilityContext)
            .Single();

        var headers = await DispatchWithAmbientActivity(routingContext);

        AssertTraceHeadersPreserved(headers);
    }

    [Test]
    public async Task Should_preserve_trace_headers_when_delayed_retrying()
    {
        var recoverabilityContext = CreateRecoverabilityContext();
        var routingContext = new DelayedRetry(TimeSpan.FromSeconds(1))
            .GetRoutingContexts(recoverabilityContext)
            .Single();

        var headers = await DispatchWithAmbientActivity(routingContext);

        AssertTraceHeadersPreserved(headers);
    }

    [Test]
    public async Task Should_preserve_trace_headers_when_auditing()
    {
        var auditContext = new TestableAuditContext
        {
            Message = new OutgoingMessage("message-id", CreateReceivedHeaders(), ReadOnlyMemory<byte>.Empty)
        };
        var routingContext = RouteToAudit.Instance
            .GetRoutingContexts(auditContext)
            .Single();

        var headers = await DispatchWithAmbientActivity(routingContext);

        AssertTraceHeadersPreserved(headers);
    }

    [Test]
    public async Task Should_preserve_trace_headers_when_forwarding_current_message()
    {
        var receivedHeaders = CreateReceivedHeaders();
        var incomingMessage = new IncomingMessage("message-id", receivedHeaders, ReadOnlyMemory<byte>.Empty);
        var incomingContext = new TestableIncomingLogicalMessageContext();
        incomingContext.Extensions.Set(incomingMessage);

        Dictionary<string, string> headers = null;
        var routingPipelineCache = new RoutingPipelineCache(async routingContext =>
            headers = await DispatchWithAmbientActivity(routingContext));
        incomingContext.Extensions.Set<IPipelineCache>(routingPipelineCache);

        await IncomingMessageOperations.ForwardCurrentMessageTo(incomingContext, "destination");

        AssertTraceHeadersPreserved(headers);
        // The forwarded message shares the header dictionary of the message being processed
        AssertTraceHeadersPreserved(receivedHeaders);
    }

    static TestableRecoverabilityContext CreateRecoverabilityContext() =>
        new()
        {
            MessageId = "message-id",
            Headers = CreateReceivedHeaders()
        };

    static Dictionary<string, string> CreateReceivedHeaders() =>
        new()
        {
            [Headers.DiagnosticsTraceParent] = OriginalTraceParent
        };

    static async Task<Dictionary<string, string>> DispatchWithAmbientActivity(IRoutingContext routingContext)
    {
        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.SetIdFormat(ActivityIdFormat.W3C);
        ambientActivity.AddBaggage("ambient-key", "ambient-value");
        ambientActivity.Start();

        var connector = new RoutingToDispatchConnector(NoOpActivityFactory.Instance);
        TransportOperation transportOperation = null;

        await connector.Invoke(routingContext, dispatchContext =>
        {
            transportOperation = dispatchContext.Operations.Single();
            return Task.CompletedTask;
        });

        return transportOperation.Message.Headers;
    }

    static void AssertTraceHeadersPreserved(Dictionary<string, string> headers)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                headers[Headers.DiagnosticsTraceParent],
                Is.EqualTo(OriginalTraceParent),
                "traceparent of the received message must be preserved");
            Assert.That(
                headers,
                Does.Not.ContainKey(Headers.DiagnosticsBaggage),
                "baggage of the current activity must not leak into the forwarded message");
        }
    }

    sealed class RoutingPipelineCache(Func<IRoutingContext, Task> routingPipeline) : IPipelineCache
    {
        public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext =>
            (IPipeline<TContext>)(object)new RoutingPipeline(routingPipeline);

        sealed class RoutingPipeline(Func<IRoutingContext, Task> invoke) : IPipeline<IRoutingContext>
        {
            public Task Invoke(IRoutingContext context) => invoke(context);
        }
    }
}
