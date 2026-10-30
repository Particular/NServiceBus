namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;

[TestFixture]
public class ControlMessageSerializerTests
{
    [Test]
    public void Should_deserialize_a_reschedule_with_a_new_payload()
    {
        var body = RescheduleBody();
        body["Payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
        body["PayloadContentType"] = "application/json";

        var request = Deserialize(body);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.IsReschedule, Is.True);
            Assert.That(request.ExpectedVersion, Is.EqualTo(3));
            Assert.That(request.DueTime, Is.EqualTo(dueTime));
            Assert.That(Encoding.UTF8.GetString(request.Payload), Is.EqualTo("{}"));
            Assert.That(request.PayloadContentType, Is.EqualTo("application/json"));
        }
    }

    [Test]
    public void Should_keep_the_payload_when_there_is_none() =>
        Assert.That(Deserialize(RescheduleBody()).Payload, Is.Null);

    [Test]
    public void Should_reject_a_payload_without_a_content_type()
    {
        var body = RescheduleBody();
        body["Payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));

        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => Deserialize(body));
    }

    [Test]
    public void Should_reject_a_reschedule_without_a_due_time()
    {
        var body = RescheduleBody();
        body.Remove("DueTime");

        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => Deserialize(body));
    }

    [Test]
    public void Should_reject_a_cancel_with_a_payload()
    {
        var body = RescheduleBody();
        body["Change"] = SagaTimeoutManagementHeaders.CancelChange;
        body["Payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
        body["PayloadContentType"] = "application/json";

        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => Deserialize(body));
    }

    [TestCase("Change", "Trigger")]
    [TestCase("SagaId", "00000000-0000-0000-0000-000000000000")]
    [TestCase("ExpectedVersion", -1)]
    public void Should_reject_an_invalid_value(string property, object value)
    {
        var body = RescheduleBody();
        body[property] = value is int number ? JsonValue.Create(number) : JsonValue.Create((string)value);

        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => Deserialize(body));
    }

    [Test]
    public void Should_not_deserialize_unknown_message_types() =>
        Assert.Throws<InvalidOperationException>(() => new ControlMessageSerializer().Deserialize(Encoding.UTF8.GetBytes(RescheduleBody().ToJsonString()), [typeof(object)]));

    static SagaTimeoutChangeRequest Deserialize(JsonObject body) =>
        (SagaTimeoutChangeRequest)new ControlMessageSerializer().Deserialize(Encoding.UTF8.GetBytes(body.ToJsonString()), [typeof(SagaTimeoutChangeRequest)])[0];

    JsonObject RescheduleBody() => new()
    {
        ["OperationId"] = Guid.NewGuid(),
        ["SagaType"] = "Sales.OrderPolicy",
        ["SagaId"] = Guid.NewGuid(),
        ["TimeoutId"] = "timeout-1",
        ["ExpectedVersion"] = 3,
        ["Change"] = SagaTimeoutManagementHeaders.RescheduleChange,
        ["DueTime"] = dueTime
    };

    readonly DateTimeOffset dueTime = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
}
