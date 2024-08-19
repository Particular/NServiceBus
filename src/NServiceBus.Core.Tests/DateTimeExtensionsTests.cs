namespace NServiceBus.Core.Tests;

using System;
using System.Globalization;
using NUnit.Framework;

[TestFixture]
class DateTimeExtensionsTests
{
    [Test]
    public void When_roundtripping_constructed_date_should_be_equal()
    {
        var date = new DateTimeOffset(2016, 8, 29, 16, 37, 25, 75, TimeSpan.Zero);
        var dateString = DateTimeOffsetHelper.ToWireFormattedString(date);
        var result = DateTimeOffsetHelper.ToDateTimeOffset(dateString);

        Assert.That(result, Is.EqualTo(date));
    }

    [Test]
    public void When_roundtripping_UtcNow_should_be_accurate_to_microseconds()
    {
        var date = DateTimeOffset.UtcNow;
        var dateString = DateTimeOffsetHelper.ToWireFormattedString(date);
        var result = DateTimeOffsetHelper.ToDateTimeOffset(dateString);

        Assert.Multiple(() =>
        {
            Assert.That(result.Year, Is.EqualTo(date.Year));
            Assert.That(result.Month, Is.EqualTo(date.Month));
            Assert.That(result.Day, Is.EqualTo(date.Day));
            Assert.That(result.Hour, Is.EqualTo(date.Hour));
            Assert.That(result.Minute, Is.EqualTo(date.Minute));
            Assert.That(result.Second, Is.EqualTo(date.Second));
            Assert.That(result.Millisecond, Is.EqualTo(date.Millisecond));
            Assert.That(result.Microseconds(), Is.EqualTo(date.Microseconds()));
            Assert.That(result.Offset, Is.EqualTo(date.Offset));
        });
    }

    [Test]
    public void When_converting_string_should_be_accurate_to_microseconds()
    {
        var dateString = "2016-08-16 10:06:20:123456 Z";
        var date = DateTimeOffset.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss:ffffff Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        var result = DateTimeOffsetHelper.ToDateTimeOffset(dateString);

        Assert.Multiple(() =>
        {
            Assert.That(result.Year, Is.EqualTo(date.Year));
            Assert.That(result.Month, Is.EqualTo(date.Month));
            Assert.That(result.Day, Is.EqualTo(date.Day));
            Assert.That(result.Hour, Is.EqualTo(date.Hour));
            Assert.That(result.Minute, Is.EqualTo(date.Minute));
            Assert.That(result.Second, Is.EqualTo(date.Second));
            Assert.That(result.Millisecond, Is.EqualTo(date.Millisecond));
            Assert.That(result.Microseconds(), Is.EqualTo(date.Microseconds()));
            Assert.That(result.Offset, Is.EqualTo(date.Offset));
        });
    }

    [Test]
    public void When_converting_string_that_is_too_short_should_throw()
    {
        var dateString = "201-08-16 10:06:20:123456 Z";

        var exception = Assert.Throws<FormatException>(() => DateTimeOffsetHelper.ToDateTimeOffset(dateString));
        Assert.That(exception.Message, Is.EqualTo("String was not recognized as a valid DateTime."));
    }

    [Test]
    public void When_converting_string_with_invalid_characters_should_throw()
    {
        var dateString = "201j-08-16 10:06:20:123456 Z";

        var exception = Assert.Throws<FormatException>(() => DateTimeOffsetHelper.ToDateTimeOffset(dateString));
        Assert.That(exception.Message, Is.EqualTo("String was not recognized as a valid DateTime."));
    }
}
