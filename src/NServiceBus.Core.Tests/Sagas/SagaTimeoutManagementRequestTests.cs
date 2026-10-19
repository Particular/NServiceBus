namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

[TestFixture]
public class SagaTimeoutManagementRequestTests
{
    [Test]
    public void Should_parse_a_reschedule_with_a_new_payload()
    {
        var headers = RescheduleHeaders();
        headers[Headers.ContentType] = "application/json";

        var request = SagaTimeoutManagementRequest.Parse(headers, Encoding.UTF8.GetBytes("{}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.IsReschedule, Is.True);
            Assert.That(request.ExpectedVersion, Is.EqualTo(3));
            Assert.That(request.DueTime, Is.EqualTo(dueTime));
            Assert.That(request.PayloadContentType, Is.EqualTo("application/json"));
        }
    }

    [Test]
    public void Should_keep_the_payload_when_the_body_is_empty() =>
        Assert.That(SagaTimeoutManagementRequest.Parse(RescheduleHeaders(), ReadOnlyMemory<byte>.Empty).PayloadContentType, Is.Null);

    [Test]
    public void Should_reject_a_payload_without_a_content_type() =>
        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => SagaTimeoutManagementRequest.Parse(RescheduleHeaders(), Encoding.UTF8.GetBytes("{}")));

    [Test]
    public void Should_reject_a_reschedule_without_a_due_time()
    {
        var headers = RescheduleHeaders();
        headers.Remove(SagaTimeoutManagementHeaders.DueTime);

        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => SagaTimeoutManagementRequest.Parse(headers, ReadOnlyMemory<byte>.Empty));
    }

    [Test]
    public void Should_reject_a_cancel_with_a_payload()
    {
        var headers = RescheduleHeaders();
        headers[SagaTimeoutManagementHeaders.Change] = SagaTimeoutManagementHeaders.CancelChange;
        headers[Headers.ContentType] = "application/json";

        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => SagaTimeoutManagementRequest.Parse(headers, Encoding.UTF8.GetBytes("{}")));
    }

    [TestCase(SagaTimeoutManagementHeaders.Change, "Trigger")]
    [TestCase(SagaTimeoutManagementHeaders.SagaId, "not-a-guid")]
    [TestCase(SagaTimeoutManagementHeaders.ExpectedVersion, "-1")]
    [TestCase(SagaTimeoutManagementHeaders.DueTime, "2026-10-19T10:00:00Z")]
    public void Should_reject_an_invalid_header(string header, string value)
    {
        var headers = RescheduleHeaders();
        headers[header] = value;

        Assert.Throws<InvalidSagaTimeoutManagementRequestException>(() => SagaTimeoutManagementRequest.Parse(headers, ReadOnlyMemory<byte>.Empty));
    }

    Dictionary<string, string> RescheduleHeaders() => new()
    {
        [SagaTimeoutManagementHeaders.OperationId] = Guid.NewGuid().ToString(),
        [SagaTimeoutManagementHeaders.SagaType] = "Sales.OrderPolicy",
        [SagaTimeoutManagementHeaders.SagaId] = Guid.NewGuid().ToString(),
        [SagaTimeoutManagementHeaders.TimeoutId] = "timeout-1",
        [SagaTimeoutManagementHeaders.ExpectedVersion] = "3",
        [SagaTimeoutManagementHeaders.Change] = SagaTimeoutManagementHeaders.RescheduleChange,
        [SagaTimeoutManagementHeaders.DueTime] = DateTimeOffsetHelper.ToWireFormattedString(dueTime)
    };

    readonly DateTimeOffset dueTime = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
}
