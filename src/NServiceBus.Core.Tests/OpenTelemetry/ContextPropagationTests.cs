namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class ContextPropagationTests
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

        Assert.That(headers.ContainsKey(Headers.StartNewTrace), Is.True, bool.TrueString);
        Assert.That(bool.TrueString, Is.EqualTo(headers[Headers.StartNewTrace]));
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

    [TestCaseSource(nameof(TestCases))]
    public void Can_propagate_baggage_from_header_to_activity(ContextPropagationTestCase testCase)
    {
        TestContext.WriteLine($"Baggage header: {testCase.BaggageHeaderValue}");

        var headers = new Dictionary<string, string>();

        if (testCase.HasBaggage)
        {
            headers[Headers.DiagnosticsBaggage] = testCase.BaggageHeaderValue;
        }

        var activity = new Activity(ActivityNames.IncomingMessageActivityName);

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
        TestContext.WriteLine($"Baggage header: {testCase.BaggageHeaderValue}");

        var headers = new Dictionary<string, string>();

        var activity = new Activity(ActivityNames.OutgoingMessageActivityName);

        foreach (var baggageItem in testCase.ExpectedBaggageItems.Reverse())
        {
            activity.AddBaggage(baggageItem.Key, baggageItem.Value);
        }

        ContextPropagation.PropagateContextToHeaders(activity, headers, new ContextBag());

        var baggageHeaderSet = headers.TryGetValue(Headers.DiagnosticsBaggage, out var baggageValue);

        if (testCase.HasBaggage)
        {
            Assert.That(baggageHeaderSet, Is.True, "Should have a baggage header if there is baggage");

            Assert.That(baggageValue, Is.EqualTo(testCase.BaggageHeaderValueWithoutOptionalWhitespace), "baggage header is set but is not correct");
        }
        else
        {
            Assert.That(baggageHeaderSet, Is.False, "baggage header should not be set if there is no baggage");
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Can_roundtrip_baggage(ContextPropagationTestCase testCase)
    {
        TestContext.WriteLine($"Baggage header: {testCase.BaggageHeaderValue}");

        var outgoingHeaders = new Dictionary<string, string>();
        var outgoingActivity = new Activity(ActivityNames.OutgoingMessageActivityName);

        foreach (var baggageItem in testCase.ExpectedBaggageItems.Reverse())
        {
            outgoingActivity.AddBaggage(baggageItem.Key, baggageItem.Value);
        }

        ContextPropagation.PropagateContextToHeaders(outgoingActivity, outgoingHeaders, new ContextBag());

        // Simulate wire transfer
        var incomingHeaders = outgoingHeaders;
        var incomingActivity = new Activity(ActivityNames.IncomingMessageActivityName);

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
            .WithBaggage("key1", "value1"),

        new ContextPropagationTestCase("with multiple keys")
            .WithBaggage("key1", "value1")
            .WithBaggage("key2", "value2"),

        new ContextPropagationTestCase("with whitespace")
            .WithBaggage("key1 ", " value1")
            .WithBaggage(" key2", "value2 ")
            .WithBaggage(" key3 ", " value3 "),

        new ContextPropagationTestCase("with properties that do not have keys")
            .WithBaggage("key1", "value1;property1;property2"),

        new ContextPropagationTestCase("with properties that have keys")
            .WithBaggage("key3", "value3; propertyKey=propertyValue"),

        new ContextPropagationTestCase("with values containing whitespace")
            .WithBaggage("serverNode", "DF 28"),

        new ContextPropagationTestCase("with values containing unicode")
            .WithBaggage("userId", "Amélie")
    };

    public class ContextPropagationTestCase
    {
        string caseName;
        Dictionary<string, string> baggageItems = [];

        public ContextPropagationTestCase(string caseName)
        {
            this.caseName = caseName;
        }

        public ContextPropagationTestCase WithBaggage(string key, string value)
        {
            baggageItems.Add(key, value);
            return this;
        }

        public string BaggageHeaderValue => string.Join(",", from kvp in baggageItems select $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");
        public string BaggageHeaderValueWithoutOptionalWhitespace
            => string.Join(",", from kvp in baggageItems select $"{kvp.Key.Trim()}={Uri.EscapeDataString(kvp.Value)}");
        public IEnumerable<KeyValuePair<string, string>> ExpectedBaggageItems => from kvp in baggageItems
                                                                                 select new KeyValuePair<string, string>(
                                                                                     kvp.Key.Trim(),
                                                                                     kvp.Value
                                                                                 );

        public override string ToString() => caseName;

        public bool HasBaggage => baggageItems.Count != 0;
    }
}