// =============================================================================
// EVERYTHING IN THIS FILE IS TEMPORARY AND WILL BE REMOVED IN v11, together with
// the V11BehaviorSwitch block in NServiceBus.Core/obsoletes-v10.cs.
// =============================================================================

namespace NServiceBus.Core.Tests
{
    using System;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;

    // Runs the test with the NServiceBus.Core.OpenTelemetry.UseV11Behavior AppContext switch enabled, i.e. with
    // the OpenTelemetry defaults of v11. In v11 these defaults are the only behavior: delete this attribute and
    // remove it from the tests that use it.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
    public sealed class OpenTelemetryV11DefaultsAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test) => Set(true);

        public void AfterTest(ITest test) => Set(false);

        static void Set(bool enabled)
        {
            AppContext.SetSwitch(V11BehaviorSwitch.UseV11BehaviorSwitchName, enabled);
            V11BehaviorSwitch.ResetUseV11Behavior();
        }
    }
}

namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.Routing;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using TransportOperation = NServiceBus.Transport.TransportOperation;

    public partial class TransportReceiveToPhysicalMessageConnectorTests
    {
        // Covers the pre-v11 name of the outbox deduplication span tag. In v11 the snake_case name is the only one:
        // delete this test. Should_add_snake_case_outbox_span_tag_when_deduplicating covers the v11 name.
        [Test]
        public async Task Should_add_outbox_span_tag_when_deduplicating()
        {
            string messageId = Guid.NewGuid().ToString();
            fakeOutbox.ExistingMessage = new OutboxMessage(messageId, Array.Empty<NServiceBus.Outbox.TransportOperation>());
            var context = CreateContext(fakeBatchPipeline, messageId);

            using var pipelineActivity = new Activity("test activity");
            pipelineActivity.Start();
            context.Extensions.SetIncomingPipelineActivity(pipelineActivity);

            await Invoke(context);

            Assert.That(pipelineActivity.TagObjects.ToImmutableDictionary()["nservicebus.outbox.deduplicate-message"], Is.EqualTo(true));
        }

        [Test]
        public async Task Should_add_batch_dispatch_events_when_sending_batched_messages()
        {
            var context = CreateContext(fakeBatchPipeline, Guid.NewGuid().ToString());

            using var pipelineActivity = new Activity("test activity");
            pipelineActivity.Start();
            context.Extensions.SetIncomingPipelineActivity(pipelineActivity);

            await Invoke(context, c =>
            {
                var batchedSends = c.Extensions.Get<PendingTransportOperations>();
                batchedSends.AddRange([
                    new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination")),
                    new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination")),
                    new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination"))
                ]);
                return Task.CompletedTask;
            });

            var startDispatcherActivityEvents = pipelineActivity.Events.Where(e => e.Name == "Start dispatching").ToArray();
            Assert.That(startDispatcherActivityEvents, Has.Length.EqualTo(1));
            Assert.That(startDispatcherActivityEvents.Single().Tags.ToImmutableDictionary()["message-count"], Is.EqualTo(3));
            Assert.That(pipelineActivity.Events.Count(e => e.Name == "Finished dispatching"), Is.EqualTo(1));
        }

        // In v11 the dispatching events are gone: delete this test together with the one above and the
        // V11BehaviorSwitch block in NServiceBus.Core/obsoletes-v10.cs.
        [Test]
        [OpenTelemetryV11Defaults]
        public async Task Should_not_add_batch_dispatch_events_when_sending_batched_messages()
        {
            var context = CreateContext(fakeBatchPipeline, Guid.NewGuid().ToString());

            using var pipelineActivity = new Activity("test activity");
            pipelineActivity.Start();
            context.Extensions.SetIncomingPipelineActivity(pipelineActivity);

            await Invoke(context, c =>
            {
                var batchedSends = c.Extensions.Get<PendingTransportOperations>();
                batchedSends.Add(new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination")));
                return Task.CompletedTask;
            });

            Assert.That(pipelineActivity.Events, Is.Empty);
        }
    }
}
