namespace NServiceBus.Core.Tests
{
    using System;
    using System.Globalization;
    using NUnit.Framework;

    [TestFixture]
    class DateTimeExtensionsTests
    {
        [Test]
        public void When_roundtripping_constructed_date_should_be_equal()
        {
            var date = new DateTime(2016, 8, 29, 16, 37, 25, 75, DateTimeKind.Utc);
            var dateString = DateTimeExtensions.ToWireFormattedString(date);
            var result = DateTimeExtensions.ToUtcDateTime(dateString);

            Assert.AreEqual(date, result);
        }

        [Test]
        public void When_roundtripping_UtcNow_should_be_accurate_to_microseconds()
        {
            var date = DateTime.UtcNow;
            var dateString = DateTimeExtensions.ToWireFormattedString(date);
            var result = DateTimeExtensions.ToUtcDateTime(dateString).UtcDateTime;

            Assert.AreEqual(date.Year, result.Year);
            Assert.AreEqual(date.Month, result.Month);
            Assert.AreEqual(date.Day, result.Day);
            Assert.AreEqual(date.Hour, result.Hour);
            Assert.AreEqual(date.Minute, result.Minute);
            Assert.AreEqual(date.Second, result.Second);
            Assert.AreEqual(date.Millisecond, result.Millisecond);
            Assert.AreEqual(date.Microseconds(), result.Microseconds());
        }

        [Test]
        public void When_converting_string_should_be_accurate_to_microseconds()
        {
            var dateString = "2016-08-16 10:06:20:123456 Z";
            var date = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss:ffffff Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            var result = DateTimeExtensions.ToUtcDateTime(dateString).UtcDateTime;

            Assert.AreEqual(date.Year, result.Year);
            Assert.AreEqual(date.Month, result.Month);
            Assert.AreEqual(date.Day, result.Day);
            Assert.AreEqual(date.Hour, result.Hour);
            Assert.AreEqual(date.Minute, result.Minute);
            Assert.AreEqual(date.Second, result.Second);
            Assert.AreEqual(date.Millisecond, result.Millisecond);
            Assert.AreEqual(date.Microseconds(), result.Microseconds());
        }

        [Test]
        public void When_converting_string_that_is_too_short_should_throw()
        {
            var dateString = "201-08-16 10:06:20:123456 Z";

            var exception = Assert.Throws<FormatException>(() => DateTimeExtensions.ToUtcDateTime(dateString));
            Assert.AreEqual(exception.Message, "String was not recognized as a valid DateTime.");
        }

        [Test]
        public void When_converting_string_with_invalid_characters_should_throw()
        {
            var dateString = "201j-08-16 10:06:20:123456 Z";

            var exception = Assert.Throws<FormatException>(() => DateTimeExtensions.ToUtcDateTime(dateString));
            Assert.AreEqual(exception.Message, "String was not recognized as a valid DateTime.");
        }
    }
}
