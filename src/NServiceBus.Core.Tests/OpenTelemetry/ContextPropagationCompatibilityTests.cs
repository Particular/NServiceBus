namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class ContextPropagationCompatibilityTests
{
    // The "New*" delegates route through ContextPropagation, whose DistributedContextPropagator
    // path is opt-in until v11, so the switch must be enabled for these tests.
    [SetUp]
    public void EnableDistributedContextPropagator()
    {
        AppContext.SetSwitch(LegacyContextPropagation.UseDistributedContextPropagatorSwitchName, true);
        LegacyContextPropagation.ResetUseDistributedContextPropagator();
    }

    [TearDown]
    public void ResetDistributedContextPropagator()
    {
        AppContext.SetSwitch(LegacyContextPropagation.UseDistributedContextPropagatorSwitchName, false);
        LegacyContextPropagation.ResetUseDistributedContextPropagator();
    }

    delegate void Writer(Activity activity, Dictionary<string, string> headers, ContextBag context);
    delegate void Reader(Activity activity, IDictionary<string, string> headers);

    static readonly Writer LegacyWrite = LegacyContextPropagator.PropagateContextToHeaders;
    static readonly Reader LegacyRead = LegacyContextPropagator.PropagateContextFromHeaders;
    static readonly Writer NewWrite = ContextPropagation.PropagateContextToHeaders;
    static readonly Reader NewRead = ContextPropagation.PropagateContextFromHeaders;

    // A value exercising every class of special character: structural baggage delimiters
    // (',' ';' '='), the escape char '%', quotes, brackets, slashes, ampersand, Unicode and
    // an emoji, plus interior spaces. Deliberately has NO leading/trailing whitespace, so this
    // value isolates "what happens to special characters" from the separate edge-whitespace
    // issue covered by New_propagation_loses_leading_whitespace_in_a_value.
    // This already includes property-like syntax (the ';' and '=' delimiters), so a value such as
    // "zone=eu;sensitive" is just a subset and needs no separate case here.
    const string AllSpecialCharacters = "a b,c;d=e&f'g\"h\\i(j)k{l}m[n]o%p/q?r:s@t~u|v<w>x é ü 😀 z";

    static Dictionary<string, string> Send(string value, Writer write)
    {
        using var sender = new Activity(ActivityNames.OutgoingMessageActivityName);
        sender.SetIdFormat(ActivityIdFormat.W3C);
        sender.Start();
        sender.AddBaggage("key", value);

        var headers = new Dictionary<string, string>();
        write(sender, headers, new ContextBag());
        sender.Stop();
        return headers;
    }

    static string Receive(Dictionary<string, string> headers, Reader read)
    {
        using var receiver = new Activity(ActivityNames.IncomingMessageActivityName);
        receiver.SetIdFormat(ActivityIdFormat.W3C);
        receiver.Start();
        read(receiver, headers);
        return receiver.GetBaggageItem("key");
    }

    static string Transmit(string value, Writer write, Reader read) => Receive(Send(value, write), read);

    [Test]
    public void Legacy_sender_to_new_receiver_preserves_the_value()
    {
        var received = Transmit(AllSpecialCharacters, LegacyWrite, NewRead);
        Assert.That(received, Is.EqualTo(AllSpecialCharacters));
    }

    [Test]
    public void New_sender_to_legacy_receiver_prepends_a_leading_space_but_keeps_the_special_characters()
    {
        var received = Transmit(AllSpecialCharacters, NewWrite, LegacyRead);

        Assert.That(received, Is.EqualTo(" " + AllSpecialCharacters),
                "ignoring the leading space, every special character round-trips correctly");
    }

    [Test]
    public void New_propagation_loses_leading_whitespace_in_a_value()
    {
        const string valueWithLeadingSpace = " hasLeadingSpace";

        var legacyRoundTrip = Transmit(valueWithLeadingSpace, LegacyWrite, LegacyRead);
        var newRoundTrip = Transmit(valueWithLeadingSpace, NewWrite, NewRead);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(legacyRoundTrip, Is.EqualTo(valueWithLeadingSpace),
                "legacy propagation preserves leading whitespace via percent-encoding");
            Assert.That(newRoundTrip, Is.EqualTo("hasLeadingSpace"),
                "new propagation strips the leading whitespace from the value");
        }
    }

    [TestCase(null, "", "")]
    [TestCase("", "", "")]
    [TestCase("    ", "", "    ")]
    [TestCase("  x  ", "x", "  x  ")]
    [TestCase("  x x  ", "x x", "  x x  ")]
    public void ValidateThatLegacyPropagatorPreservesLeadingAndTrailingWhitespaceInBaggageValues(string input, string expectedNew, string expectedLegacy)
    {
        var outputNew = Transmit(input, NewWrite, NewRead);
        var outputLegacy = Transmit(input, LegacyWrite, LegacyRead);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(expectedNew, Is.EqualTo(outputNew), "Native propagator isn't trimming all leading and trailing whitespaces");
            Assert.That(expectedLegacy, Is.EqualTo(outputLegacy), "Legacy propagator isn't preserving leading and trailing whitespace for backwards compatibility");
        }
    }
}