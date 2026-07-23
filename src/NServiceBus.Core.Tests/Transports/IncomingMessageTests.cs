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
    public void RevertToOriginalHeadersIfNeeded_should_restore_original_headers()
    {
        var headers = new Dictionary<string, string>
        {
            { Headers.MessageId, "id" },
            { Headers.ContentType, "text/plain" },
            { "RemovedHeader", "original" }
        };
        var message = new IncomingMessage("id", headers, System.Array.Empty<byte>());

        message.Headers[Headers.ContentType] = "application/json";
        message.Headers["AddedHeader"] = "added";
        message.Headers.Remove("RemovedHeader");

        message.RevertToOriginalHeadersIfNeeded();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Headers[Headers.ContentType], Is.EqualTo("text/plain"));
            Assert.That(message.Headers.ContainsKey("AddedHeader"), Is.False);
            Assert.That(message.Headers["RemovedHeader"], Is.EqualTo("original"));
        }
    }
}