namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_a_delayed_message : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_start_new_trace_on_receive_by_default()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s => s.Send(new DelayedMessage(), DelayedSend())))
            .Run();

        var (send, receive) = GetActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receive.TraceId, Is.Not.EqualTo(send.TraceId), "a delayed send should start a new trace on receive by default (backward compatible)");
            Assert.That(receive.ParentId, Is.Null, "receive should be a new root");
        }

        var link = receive.Links.FirstOrDefault();
        Assert.That(link, Is.Not.Default, "receive should be linked back to the send operation");
        Assert.That(link.Context.TraceId, Is.EqualTo(send.TraceId));
    }

    [Test]
    public async Task Should_continue_existing_trace_on_receive_when_configured()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b.CustomConfig(c =>
                {
                    c.Tracing().DelayedDelivery.SendOperationTraceMode = TraceMode.ContinueExisting;
                })
                .When(s => s.Send(new DelayedMessage(), DelayedSend())))
            .Run();

        var (send, receive) = GetActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receive.TraceId, Is.EqualTo(send.TraceId), "a delayed send should continue the existing trace when SendOperationTraceMode is set to ContinueExisting");
            Assert.That(receive.ParentId, Is.EqualTo(send.Id));
            Assert.That(receive.Links, Is.Empty);
        }
    }

    [Test]
    public async Task Should_start_new_trace_by_default_no_matter_per_message_option()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s =>
                {
                    var sendOptions = DelayedSend();
                    sendOptions.ContinueExistingTraceOnReceive();
                    return s.Send(new DelayedMessage(), sendOptions);
                }))
            .Run();

        var (send, receive) = GetActivities();

        Assert.That(receive.TraceId, Is.Not.EqualTo(send.TraceId),
            "a per-message request to continue the existing trace must not defeat the backward-compatible default for delayed sends");
    }

    static SendOptions DelayedSend()
    {
        var sendOptions = new SendOptions();
        sendOptions.RouteToThisEndpoint();
        sendOptions.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
        return sendOptions;
    }

    (System.Diagnostics.Activity Send, System.Diagnostics.Activity Receive) GetActivities()
    {
        var sendActivities = NServiceBusActivityListener.CompletedActivities.GetSendMessageActivities();
        var receiveActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sendActivities, Has.Count.EqualTo(1), "1 message is sent as part of this test");
            Assert.That(receiveActivities, Has.Count.EqualTo(1), "1 message is received as part of this test");
        }

        return (sendActivities[0], receiveActivities[0]);
    }

    public class Context : ScenarioContext
    {
        public bool DelayedMessageReceived { get; set; }
    }

    public class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (c, _) => { }, metadata => { });
        }

        [Handler]
        public class DelayedMessageHandler(Context testContext) : IHandleMessages<DelayedMessage>
        {
            public Task Handle(DelayedMessage message, IMessageHandlerContext context)
            {
                testContext.DelayedMessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class DelayedMessage : IMessage;
}
