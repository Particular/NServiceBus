namespace NServiceBus.Core.Tests.Marshaling;

using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Text;
using System.Text.Json;
using NServiceBus;
using Testing;
using Transport;

[TestFixture]
public class CloudEventJsonStructuredUnmarshalerTests
{
    private string NativeMessageId;
    private Dictionary<string, string> NativeHeaders;
    private Dictionary<string, object> Payload;
    private ReadOnlyMemory<byte> Body;
    private CloudEventJsonStructuredUnmarshaler Unmarshaler;

    [SetUp]
    public void SetUp()
    {
        NativeMessageId = Guid.NewGuid().ToString();
        Payload = new Dictionary<string, object> {
            ["type"] = "com.example.someevent",
            ["source"] = "/mycontext",
            ["id"] = Guid.NewGuid().ToString()
        };
        NativeHeaders = new Dictionary<string, string>
        {
            [Headers.ContentType] = "application/cloudevents+json",
        };
        Body = new ReadOnlyMemory<byte>();
        Unmarshaler = new CloudEventJsonStructuredUnmarshaler();
    }

    [Test]
    public void Should_unmarshal_regular_json()
    {
        var cloudEventBody = new Dictionary<string, string> {
            ["property"] = "value"
        };
        Payload["datacontenttype"] = "application/json";
        Payload["data"] = cloudEventBody;

        IncomingMessage actual = RunUnmarshalTest();

        Assert.Multiple (() =>
        {
            Assert.That(actual.MessageId, Is.EqualTo(Payload["id"]));
            // TODO Assert.That(actual.NativeMessageId, Is.EqualTo(NativeMessageId));
            Assert.That(actual.Headers["type"], Is.EqualTo(Payload["type"]));
            Assert.That(actual.Headers["source"], Is.EqualTo(Payload["source"]));
            Assert.That(actual.Headers["datacontenttype"], Is.EqualTo(Payload["datacontenttype"]));
            Assert.That(actual.Body.Span.SequenceEqual(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cloudEventBody))));
        });
    }

    [Test]
    public void Should_unmarshal_regular_xml()
    {
        Payload["datacontenttype"] = "application/xml";
        Payload["data"] = "<much wow=\"xml\"/>";

        IncomingMessage actual = RunUnmarshalTest();

        Assert.Multiple(() =>
        {
            Assert.That(actual.MessageId, Is.EqualTo(Payload["id"]));
            // TODO Assert.That(actual.NativeMessageId, Is.EqualTo(NativeMessageId));
            Assert.That(actual.Headers["type"], Is.EqualTo(Payload["type"]));
            Assert.That(actual.Headers["source"], Is.EqualTo(Payload["source"]));
            Assert.That(actual.Headers["datacontenttype"], Is.EqualTo(Payload["datacontenttype"]));
            Assert.That(actual.Body.Span.SequenceEqual(Encoding.UTF8.GetBytes(Payload["data"].ToString())));
        });
    }

    [Test]
    public void Should_unmarshal_base64_binary()
    {
        var rawPayload = "<much wow=\"xml\"/>";
        Payload["datacontenttype"] = "application/xml";
        Payload["data_base64"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawPayload));

        IncomingMessage actual = RunUnmarshalTest();

        Assert.Multiple(() =>
        {
            Assert.That(actual.MessageId, Is.EqualTo(Payload["id"]));
            // TODO Assert.That(actual.NativeMessageId, Is.EqualTo(NativeMessageId));
            Assert.That(actual.Headers["type"], Is.EqualTo(Payload["type"]));
            Assert.That(actual.Headers["source"], Is.EqualTo(Payload["source"]));
            Assert.That(actual.Headers["datacontenttype"], Is.EqualTo(Payload["datacontenttype"]));
            Assert.That(actual.Body.Span.SequenceEqual(Encoding.UTF8.GetBytes(rawPayload)));
        });
    }

    private IncomingMessage RunUnmarshalTest()
    {
        string serializedBody = JsonSerializer.Serialize(Payload);
        var fullBody = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(serializedBody));
        var context = new TestableMessageContext(NativeMessageId, NativeHeaders, fullBody);
        return Unmarshaler.CreateIncomingMessage(context);
    }
}