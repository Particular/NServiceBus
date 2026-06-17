namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class ContextPropagationDefaultBehaviorTests
{
    // Without the opt-in switch, the endpoint default must remain the backwards-compatible
    // legacy propagator (percent-encoded, comma-separated, whitespace preserved).
    [SetUp]
    public void EnsureDefault()
    {
        AppContext.SetSwitch(ObsoleteV11.UseDistributedContextPropagatorSwitchName, false);
        ObsoleteV11.ResetUseDistributedContextPropagator();
    }

    [Test]
    public void Default_uses_legacy_percent_encoded_baggage_format()
    {
        using var activity = new Activity(ActivityNames.OutgoingMessageActivityName);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.AddBaggage("serverNode", "DF 28");

        var headers = new Dictionary<string, string>();
        ContextPropagation.PropagateContextToHeaders(activity, headers, new ContextBag());

        Assert.That(headers[Headers.DiagnosticsBaggage], Is.EqualTo("serverNode=DF%2028"));
    }

    [Test]
    public void Default_round_trip_preserves_value_whitespace()
    {
        using var outgoing = new Activity(ActivityNames.OutgoingMessageActivityName);
        outgoing.SetIdFormat(ActivityIdFormat.W3C);
        outgoing.Start();
        outgoing.AddBaggage("key1", " leading-and-trailing ");

        var headers = new Dictionary<string, string>();
        ContextPropagation.PropagateContextToHeaders(outgoing, headers, new ContextBag());

        using var incoming = new Activity(ActivityNames.IncomingMessageActivityName);
        incoming.SetIdFormat(ActivityIdFormat.W3C);
        incoming.Start();
        ContextPropagation.PropagateContextFromHeaders(incoming, headers);

        // Legacy propagation preserves leading/trailing whitespace via percent-encoding;
        // the DistributedContextPropagator (opt-in) would trim it.
        Assert.That(incoming.GetBaggageItem("key1"), Is.EqualTo(" leading-and-trailing "));
    }
}
