﻿namespace NServiceBus.Core.Tests.Marshaling;

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
    string NativeMessageId;
    Dictionary<string, string> NativeHeaders;
    Dictionary<string, object> Payload;
    ReadOnlyMemory<byte> Body;
    CloudEventJsonStructuredUnmarshaler Unmarshaler;

    [SetUp]
    public void SetUp()
    {
        NativeMessageId = Guid.NewGuid().ToString();
        Payload = new Dictionary<string, object>
        {
            ["type"] = "com.example.someevent",
            ["source"] = "/mycontext",
            ["id"] = Guid.NewGuid().ToString(),
            ["some_other_property"] = "some_other_value",
            ["data"] = "{}",
            ["datacontenttype"] = "application/json"
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
        var cloudEventBody = new Dictionary<string, string>
        {
            ["property"] = "value"
        };
        Payload["datacontenttype"] = "application/json";
        Payload["data"] = cloudEventBody;

        IncomingMessage actual = RunUnmarshalTest();

        Assert.Multiple(() =>
        {
            AssertTypicalFields(actual);
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
            AssertTypicalFields(actual);
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
            AssertTypicalFields(actual);
            Assert.That(actual.Body.Span.SequenceEqual(Encoding.UTF8.GetBytes(rawPayload)));
        });
    }

    [Test]
    public void Should_support_message_with_correct_content_type()
    {
        var actual = Unmarshaler.IsValidMessage(new TestableMessageContext(NativeMessageId, NativeHeaders, Body));
        Assert.That(actual, Is.True);
    }

    [Test]
    public void Should_not_support_message_with_incorrect_content_type()
    {
        NativeHeaders[Headers.ContentType] = "wrong_content";
        var actual = Unmarshaler.IsValidMessage(new TestableMessageContext(NativeMessageId, NativeHeaders, Body));
        Assert.That(actual, Is.False);
    }

    [Test]
    [TestCase("type")]
    [TestCase("id")]
    [TestCase("datacontenttype")]
    public void Should_throw_when_property_is_missing(string property)
    {
        Assert.Throws<NotSupportedException>(() =>
        {
            Payload.Remove(property);
            RunUnmarshalTest();
        });
    }

    [Test]
    public void Should_throw_when_data_properties_are_missing()
    {
        Assert.Throws<NotSupportedException>(() =>
        {
            Payload.Remove("data");
            Payload.Remove("data_base64");
            RunUnmarshalTest();
        });
    }

    [Test]
    public void Should_not_throw_when_data_property_is_present()
    {
        Assert.DoesNotThrow(() =>
        {
            Payload["data"] = "{}";
            Payload.Remove("data_base64");
            RunUnmarshalTest();
        });
    }

    [Test]
    public void Should_not_throw_when_data_base64_property_is_present()
    {
        Assert.DoesNotThrow(() =>
        {
            Payload["data_base64"] = "e30=";
            Payload.Remove("data");
            RunUnmarshalTest();
        });
    }

    IncomingMessage RunUnmarshalTest()
    {
        string serializedBody = JsonSerializer.Serialize(Payload);
        var fullBody = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(serializedBody));
        var context = new TestableMessageContext(NativeMessageId, NativeHeaders, fullBody);
        return Unmarshaler.CreateIncomingMessage(context);
    }

    void AssertTypicalFields(IncomingMessage actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.MessageId, Is.EqualTo(Payload["id"]));
            Assert.That(actual.NativeMessageId, Is.EqualTo(NativeMessageId));
            Assert.That(actual.Headers[Headers.MessageId], Is.EqualTo(Payload["id"]));
            Assert.That(actual.Headers["id"], Is.EqualTo(Payload["id"]));
            Assert.That(actual.Headers["type"], Is.EqualTo(Payload["type"]));
            Assert.That(actual.Headers["source"], Is.EqualTo(Payload["source"]));
            Assert.That(actual.Headers["datacontenttype"], Is.EqualTo(Payload["datacontenttype"]));
            Assert.That(actual.Headers["some_other_property"], Is.EqualTo(Payload["some_other_property"]));
            Assert.That(actual.Headers.ContainsKey("data"), Is.False);
            Assert.That(actual.Headers.ContainsKey("data_base64"), Is.False);
        });
    }
}