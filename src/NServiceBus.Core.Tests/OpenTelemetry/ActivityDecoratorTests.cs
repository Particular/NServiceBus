namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class ActivityDecoratorTests
{
    [Test]
    public void Verify_promotable_headers()
    {
        var headers = typeof(Headers).GetFields(BindingFlags.Public | BindingFlags.Static);

        var promotedStringBuilder = new StringBuilder();
        var ignoredStringBuilder = new StringBuilder();

        foreach (var headerField in headers)
        {
            var headerValue = headerField.GetValue(null).ToString();
            if (ActivityDecorator.HeaderMapping.TryGetValue(headerValue, out var tagName))
            {
                promotedStringBuilder.AppendLine($"{nameof(Headers)}.{headerField.Name}: {headerValue} -> {tagName}");
            }
            else
            {
                ignoredStringBuilder.AppendLine($"{nameof(Headers)}.{headerField.Name}");
            }
        }

        Approver.Verify(promotedStringBuilder.ToString(), scenario: "promoted");
        Approver.Verify(ignoredStringBuilder.ToString(), scenario: "ignored");
    }

    [Test]
    public void PromoteHeadersToTags_should_promote_promotable_headers_to_tags()
    {
        var activity = new Activity("test");
        var headers = new Dictionary<string, string>()
        {
            {Headers.MessageId, "message id"},
            {Headers.ControlMessageHeader, "control message header"},
            {"NServiceBus.UnknownMessageHeader", "unknown header"},
            {"SomeOtherHeader", "some other header"}
        };

        ActivityDecorator.PromoteHeadersToTags(activity, headers);

        var tags = activity.Tags.ToImmutableDictionary();

        Assert.That(tags.Count, Is.EqualTo(2), "should only contain approved NServiceBus headers");
        Assert.Multiple(() =>
        {
            Assert.That(tags["nservicebus.message_id"], Is.EqualTo(headers[Headers.MessageId]));
            Assert.That(tags["nservicebus.control_message"], Is.EqualTo(headers[Headers.ControlMessageHeader]));
        });
    }
}