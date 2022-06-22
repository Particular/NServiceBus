namespace NServiceBus.Core.Tests.Diagnostics;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using NUnit.Framework;

[TestFixture]
public class ActivityDecoratorTests
{
    [Test]
    public void PromoteHeadersToTags_should_promote_nservicebus_headers_to_tags()
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

        Assert.AreEqual(3, tags.Count, "should only contain NServiceBus headers");
        Assert.AreEqual(headers[Headers.MessageId], tags["nservicebus.message_id"]);
        Assert.AreEqual(headers[Headers.ControlMessageHeader], tags["nservicebus.control_message"]);
        Assert.AreEqual(headers["NServiceBus.UnknownMessageHeader"], tags["nservicebus.unknown_message_header"]);
    }

    [Test]
    public void PromoteHeadersToTags_should_not_promote_ignored_headers()
    {
        var activity = new Activity("test");
        var headers = new Dictionary<string, string>()
        {
            {Headers.TimeSent, "some value"},
        };

        ActivityDecorator.PromoteHeadersToTags(activity, headers);

        Assert.IsEmpty(activity.Tags);
    }
}