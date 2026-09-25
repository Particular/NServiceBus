#nullable enable

namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

// Covers the pre-v11 default of TransportParentSpanSwitch: without the opt-in switch the incoming
// message span stays a child of the NServiceBus sender span even when a transport SDK span is
// ambient. In v11 that default is gone, so delete this file together with obsolete_v11.cs.
[TestFixture]
public class TransportParentSpanDefaultBehaviorTests
{
    readonly ActivityFactory activityFactory = new(new InstrumentationOptions());

    TestingActivityListener nsbActivityListener;

    [SetUp]
    public void SetUp()
    {
        nsbActivityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
        AppContext.SetSwitch(TransportParentSpanSwitch.UseTransportSpanAsParentSwitchName, false);
        TransportParentSpanSwitch.ResetUseTransportSpanAsParent();
    }

    [TearDown]
    public void TearDown()
    {
        nsbActivityListener.Dispose();
        TransportParentSpanSwitch.ResetUseTransportSpanAsParent();
    }

    [Test]
    public void Default_attaches_to_header_trace_when_no_activity_on_context_and_trace_header_and_ambient_activity()
    {
        using var sendActivity = new Activity("send activity");
        sendActivity.SetIdFormat(ActivityIdFormat.W3C);
        sendActivity.Start();
        sendActivity.Stop();

        using var ambientActivity = new Activity("transport sdk receive activity");
        ambientActivity.Start();

        var messageHeaders = new Dictionary<string, string> { { Headers.DiagnosticsTraceParent, sendActivity.Id! } };
        var messageContext = new MessageContext(Guid.NewGuid().ToString(), messageHeaders, Array.Empty<byte>(), new TransportTransaction(), "receiver", new ContextBag());

        var activity = activityFactory.StartIncomingPipelineActivity(messageContext);

        Assert.That(activity, Is.Not.Null, "should create activity for receive pipeline");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.ParentId, Is.EqualTo(sendActivity.Id), "should use the sender span as parent, ignoring the ambient activity");
            Assert.That(activity.Links.Count(), Is.EqualTo(0), "should not link to logical send span");
        }
    }
}
