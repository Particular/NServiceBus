namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class LegacyContextPropagationTests
{
    [Test]
    public void Propagate_activity_id_to_header()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var headers = new Dictionary<string, string>();

        ContextPropagation.PropagateContextToHeaders(activity, headers, new ContextBag());

        Assert.That(activity.Id, Is.EqualTo(headers[Headers.DiagnosticsTraceParent]));
    }

    [Test]
    public void Should_not_set_header_without_activity()
    {
        var headers = new Dictionary<string, string>();

        ContextPropagation.PropagateContextToHeaders(null, headers, new ContextBag());

        Assert.That(headers, Is.Empty);
    }

    [Test]
    public void Should_set_start_new_trace_header_when_adding_trace_parent_header()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var headers = new Dictionary<string, string>();
        var contextBag = new ContextBag();
        contextBag.Set(Headers.StartNewTrace, bool.TrueString);
        ContextPropagation.PropagateContextToHeaders(activity, headers, contextBag);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers.ContainsKey(Headers.StartNewTrace), Is.True, bool.TrueString);
            Assert.That(bool.TrueString, Is.EqualTo(headers[Headers.StartNewTrace]));
        }
    }

    [Test]
    public void Should_not_set_start_new_trace_header_when_no_trace_parent_header_is_added()
    {
        var headers = new Dictionary<string, string>();
        var contextBag = new ContextBag();
        contextBag.Set(Headers.StartNewTrace, bool.TrueString);
        ContextPropagation.PropagateContextToHeaders(null, headers, contextBag);

        Assert.That(headers.ContainsKey(Headers.StartNewTrace), Is.False);
    }

    [Test]
    public void Overwrites_existing_propagation_header()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var headers = new Dictionary<string, string>()
        {
            { Headers.DiagnosticsTraceParent, "some existing id" }
        };

        ContextPropagation.PropagateContextToHeaders(activity, headers, new ContextBag());

        Assert.That(activity.Id, Is.EqualTo(headers[Headers.DiagnosticsTraceParent]));
    }

    [Test]
    public void Should_not_throw_when_baggage_value_is_null()
    {
        // Reproduces https://github.com/Particular/NServiceBus/issues/6983
        // A baggage item with a null value used to make the hand-written propagator call
        // Uri.EscapeDataString(null), throwing ArgumentNullException while sending a message.
        using var activity = new Activity(ActivityNames.OutgoingMessageActivityName);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.AddBaggage("test", null);

        var headers = new Dictionary<string, string>();

        Assert.DoesNotThrow(() => ContextPropagation.PropagateContextToHeaders(activity, headers, new ContextBag()));
    }

    [TestCaseSource(nameof(TestCases))]
    public void Can_propagate_baggage_from_header_to_activity(ContextPropagationTestCase testCase)
    {
        TestContext.Out.WriteLine($"Baggage header: {testCase.BaggageHeaderValue}");

        var headers = new Dictionary<string, string>();

        if (testCase.HasBaggage)
        {
            headers[Headers.DiagnosticsBaggage] = testCase.BaggageHeaderValue;
        }

        using var activity = new Activity(ActivityNames.IncomingMessageActivityName);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        ContextPropagation.PropagateContextFromHeaders(activity, headers);

        foreach (var baggageItem in testCase.ExpectedBaggageItems)
        {
            var key = baggageItem.Key;
            var actualValue = activity.GetBaggageItem(key);
            Assert.That(actualValue, Is.Not.Null, $"Baggage is missing item with key |{key}|");
            Assert.That(actualValue, Is.EqualTo(baggageItem.Value), $"Baggage item |{key}| has the wrong value");
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Can_propagate_baggage_from_activity_to_header(ContextPropagationTestCase testCase)
    {
        TestContext.Out.WriteLine($"Baggage header: {testCase.BaggageHeaderValue}");

        var headers = new Dictionary<string, string>();

        using var activity = new Activity(ActivityNames.OutgoingMessageActivityName);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        foreach (var baggageItem in testCase.ExpectedBaggageItems.Reverse())
        {
            activity.AddBaggage(baggageItem.Key, baggageItem.Value);
        }

        ContextPropagation.PropagateContextToHeaders(activity, headers, new ContextBag());

        var baggageHeaderSet = headers.TryGetValue(Headers.DiagnosticsBaggage, out var baggageValue);

        if (testCase.HasBaggage)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(baggageHeaderSet, Is.True, "Should have a baggage header if there is baggage");

                Assert.That(baggageValue, Is.EqualTo(testCase.BaggageHeaderValue), "baggage header is set but is not correct");
            }
        }
        else
        {
            Assert.That(baggageHeaderSet, Is.False, "baggage header should not be set if there is no baggage");
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Can_roundtrip_baggage(ContextPropagationTestCase testCase)
    {
        TestContext.Out.WriteLine($"Baggage header: {testCase.BaggageHeaderValue}");

        var outgoingHeaders = new Dictionary<string, string>();
        using var outgoingActivity = new Activity(ActivityNames.OutgoingMessageActivityName);
        outgoingActivity.SetIdFormat(ActivityIdFormat.W3C);
        outgoingActivity.Start();

        foreach (var baggageItem in testCase.ExpectedBaggageItems.Reverse())
        {
            outgoingActivity.AddBaggage(baggageItem.Key, baggageItem.Value);
        }

        ContextPropagation.PropagateContextToHeaders(outgoingActivity, outgoingHeaders, new ContextBag());

        // Simulate wire transfer
        var incomingHeaders = outgoingHeaders;
        using var incomingActivity = new Activity(ActivityNames.IncomingMessageActivityName);
        incomingActivity.SetIdFormat(ActivityIdFormat.W3C);
        incomingActivity.Start();

        ContextPropagation.PropagateContextFromHeaders(incomingActivity, incomingHeaders);

        foreach (var baggageItem in testCase.ExpectedBaggageItems)
        {
            var key = baggageItem.Key;
            var actualValue = incomingActivity.GetBaggageItem(key);
            Assert.That(actualValue, Is.Not.Null, $"Baggage is missing item with key |{key}|");
            Assert.That(actualValue, Is.EqualTo(baggageItem.Value), $"Baggage item |{key}| has the wrong value");
        }
    }

    // HINT: Many of these test cases are given as examples in the spec https://www.w3.org/TR/baggage/#example
    static IEnumerable TestCases => new object[]
    {
        new ContextPropagationTestCase("without any baggage"),

        new ContextPropagationTestCase("with a single key")
            .WithBaggage("key1", "value1")
            .WithHeaderValue("key1=value1"),

        new ContextPropagationTestCase("with multiple keys")
            .WithBaggage("key1", "value1")
            .WithBaggage("key2", "value2")
            .WithHeaderValue("key1=value1,key2=value2"),

        new ContextPropagationTestCase("with properties that do not have keys")
            .WithBaggage("key1", "value1;property1;property2")
            .WithHeaderValue("key1=value1%3Bproperty1%3Bproperty2"),

        new ContextPropagationTestCase("with properties that have keys")
            .WithBaggage("key3", "value3; propertyKey=propertyValue")
            .WithHeaderValue("key3=value3%3B%20propertyKey%3DpropertyValue"),

        new ContextPropagationTestCase("with values containing whitespace")
            .WithBaggage("serverNode", "DF 28")
            .WithHeaderValue("serverNode=DF%2028"),

        new ContextPropagationTestCase("with values containing unicode")
            .WithBaggage("userId", "Amélie")
            .WithHeaderValue("userId=Am%C3%A9lie")
    };

    public class ContextPropagationTestCase(string caseName)
    {
        readonly Dictionary<string, string> baggageItems = [];

        public ContextPropagationTestCase WithBaggage(string key, string value)
        {
            baggageItems.Add(key, value);
            return this;
        }

        public ContextPropagationTestCase WithHeaderValue(string headerValue)
        {
            BaggageHeaderValue = headerValue;
            return this;
        }

        public string BaggageHeaderValue { get; private set; }
        public IEnumerable<KeyValuePair<string, string>> ExpectedBaggageItems => from kvp in baggageItems
                                                                                 select new KeyValuePair<string, string>(
                                                                                     kvp.Key.Trim(),
                                                                                     kvp.Value
                                                                                 );

        public override string ToString() => caseName;

        public bool HasBaggage => baggageItems.Count != 0;
    }
}