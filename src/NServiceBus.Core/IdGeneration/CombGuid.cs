#nullable enable
namespace NServiceBus;

using System;
using System.Buffers.Binary;

/// <summary>
/// Generates a Guid using http://www.informit.com/articles/article.asp?p=25862
/// The Comb algorithm is designed to make the use of <see cref="Guid"/>s as Primary Keys, Foreign Keys, and Indexes nearly as efficient
/// as <see cref="int"/>.
/// </summary>
/// <remarks>Original Source: https://github.com/nhibernate/nhibernate-core/blob/4.0.4.GA/src/NHibernate/Id/GuidCombGenerator.cs</remarks>
static class CombGuid
{
    public static Guid Generate() =>
        // Internal use, no need for DateTimeOffset
        Generate(Guid.NewGuid(), DateTime.UtcNow);

    internal static Guid Generate(Guid original, DateTime now)
    {
        Span<byte> guidArray = stackalloc byte[16];

        if (!original.TryWriteBytes(guidArray))
        {
            guidArray = original.ToByteArray();
        }

        // Get the days and milliseconds which will be used to build the byte string
        var days = new TimeSpan(now.Ticks - BaseDateTicks);
        var timeOfDay = now.TimeOfDay;

        // Convert to a byte array
        Span<byte> daysArray = stackalloc byte[sizeof(int)];
        // Reverse the bytes to match SQL Servers ordering
        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(daysArray, days.Days);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(daysArray, days.Days);
        }
        Span<byte> milliSecondsArray = stackalloc byte[sizeof(long)];
        // Reverse the bytes to match SQL Servers ordering
        // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteInt64BigEndian(milliSecondsArray, (long)(timeOfDay.TotalMilliseconds / 3.333333));
        }
        else
        {
            BinaryPrimitives.WriteInt64LittleEndian(milliSecondsArray, (long)(timeOfDay.TotalMilliseconds / 3.333333));
        }

        // // Copy the bytes into the guid
        daysArray[^2..].CopyTo(guidArray[10..12]);
        milliSecondsArray[^4..].CopyTo(guidArray[12..16]);

        return new Guid(guidArray);
    }

    // Represents new DateTime(1900, 1, 1).Ticks, while this would be more readable having a const here instead of
    // a static field the less readable version slightly improves the throughput
    const long BaseDateTicks = 599266080000000000; // new DateTime(1900, 1, 1).Ticks
}