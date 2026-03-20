namespace NServiceBus.Core.Tests.Transports;

using System.Collections.Generic;
using NUnit.Framework;
using Transport;

[TestFixture]
public class IncomingMessageTests
{
    [Test]
    public void Should_assign_transport_message_id_when_NServiceBus_message_id_header_is_missing()
    {
        var headers = new Dictionary<string, string>();
        var message = new IncomingMessage("nativeId", headers, System.Array.Empty<byte>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.NativeMessageId, Is.EqualTo("nativeId"));
            Assert.That(message.MessageId, Is.EqualTo("nativeId"));
            Assert.That(headers[Headers.MessageId], Is.EqualTo("nativeId"));
        }
    }

    [Test]
    public void Should_retain_transport_message_id_when_NServiceBus_message_id_header_is_found()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "coreId" }
        };
        var message = new IncomingMessage("nativeId", headers, System.Array.Empty<byte>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.NativeMessageId, Is.EqualTo("nativeId"));
            Assert.That(message.MessageId, Is.EqualTo("coreId"));
        }
    }

    [Test]
    public void Should_assign_transport_message_id_when_NServiceBus_message_id_header_is_found_without_value()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "" }
        };
        var message = new IncomingMessage("nativeId", headers, System.Array.Empty<byte>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.NativeMessageId, Is.EqualTo("nativeId"));
            Assert.That(message.MessageId, Is.EqualTo("nativeId"));
        }
    }

    [Test]
    public void RevertToOriginalHeadersIfNeeded_should_restore_added_headers()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "id" },
            { "OriginalHeader", "original-value" }
        };
        var message = new IncomingMessage("id", headers, System.Array.Empty<byte>());

        message.SnapshotHeaders();
        message.Headers["AddedByMutator"] = "mutator-value";

        message.RevertToOriginal();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers.ContainsKey("AddedByMutator"), Is.False);
            Assert.That(message.Headers["OriginalHeader"], Is.EqualTo("original-value"));
        }
    }

    [Test]
    public void RevertToOriginalHeadersIfNeeded_should_restore_modified_headers()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "id" },
            { Headers.ContentType, "text/plain" }
        };
        var message = new IncomingMessage("id", headers, System.Array.Empty<byte>());

        message.SnapshotHeaders();
        message.Headers[Headers.ContentType] = "application/json";
        message.Headers[Headers.EnclosedMessageTypes] = "MyMessage";

        message.RevertToOriginal();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers[Headers.ContentType], Is.EqualTo("text/plain"));
            Assert.That(message.Headers.ContainsKey(Headers.EnclosedMessageTypes), Is.False);
        }
    }

    [Test]
    public void RevertToOriginalHeadersIfNeeded_should_restore_removed_headers()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "id" },
            { "WillBeRemoved", "value" }
        };
        var message = new IncomingMessage("id", headers, System.Array.Empty<byte>());

        message.SnapshotHeaders();
        message.Headers.Remove("WillBeRemoved");

        message.RevertToOriginal();

        Assert.That(message.Headers["WillBeRemoved"], Is.EqualTo("value"));
    }

    [Test]
    public void RevertToOriginalHeadersIfNeeded_should_noop_without_snapshot()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "id" },
            { "Key", "value" }
        };
        var message = new IncomingMessage("id", headers, System.Array.Empty<byte>());

        message.Headers["Key"] = "mutated";
        message.RevertToOriginal();

        Assert.That(message.Headers["Key"], Is.EqualTo("mutated"));
    }

    [Test]
    public void SnapshotHeaders_should_only_capture_first_snapshot()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "id" },
            { "Key", "original" }
        };
        var message = new IncomingMessage("id", headers, System.Array.Empty<byte>());

        message.SnapshotHeaders();
        message.Headers["Key"] = "first-mutation";

        // Second snapshot should not overwrite the first
        message.SnapshotHeaders();
        message.Headers["Key"] = "second-mutation";

        message.RevertToOriginal();

        Assert.That(message.Headers["Key"], Is.EqualTo("original"));
    }
}