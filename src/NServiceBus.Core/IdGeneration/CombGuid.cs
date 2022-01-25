namespace NServiceBus
{
    using System;
    using System.Buffers.Binary;
#if NETFRAMEWORK
    using System.Runtime.InteropServices;
#endif

    /// <summary>
    /// Generates a Guid using http://www.informit.com/articles/article.asp?p=25862
    /// The Comb algorithm is designed to make the use of <see cref="Guid"/>s as Primary Keys, Foreign Keys, and Indexes nearly as efficient
    /// as <see cref="int"/>.
    /// </summary>
    /// <remarks>Original Source: https://github.com/nhibernate/nhibernate-core/blob/4.0.4.GA/src/NHibernate/Id/GuidCombGenerator.cs</remarks>
    static class CombGuid
    {
        public static Guid Generate()
        {
            var newGuid = Guid.NewGuid();
            Span<byte> guidArray = stackalloc byte[16];
#if NETCOREAPP
            if (!newGuid.TryWriteBytes(guidArray))
#elif NETFRAMEWORK
            if (!MemoryMarshal.TryWrite(guidArray, ref newGuid))
#endif
            {

                guidArray = newGuid.ToByteArray();
            }

            var now = DateTime.UtcNow; // Internal use, no need for DateTimeOffset

            // Get the days and milliseconds which will be used to build the byte string
            var days = new TimeSpan(now.Ticks - BaseDateTicks);
            var timeOfDay = now.TimeOfDay;

            Span<byte> daysArray = stackalloc byte[sizeof(int)];
            // Reverse the bytes to match SQL Servers ordering
            BinaryPrimitives.WriteInt32BigEndian(daysArray, days.Days);
            Span<byte> milliSecondsArray = stackalloc byte[sizeof(long)];
            // Reverse the bytes to match SQL Servers ordering
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
            BinaryPrimitives.WriteInt64BigEndian(milliSecondsArray, (long)(timeOfDay.TotalMilliseconds / 3.333333));

            // // Copy the bytes into the guid
            daysArray.Slice(daysArray.Length - 2).CopyTo(guidArray.Slice(10, 2));
            milliSecondsArray.Slice(milliSecondsArray.Length - 4).CopyTo(guidArray.Slice(12, 4));

#if NETCOREAPP
            return new Guid(guidArray);
#elif NETFRAMEWORK
            if (!MemoryMarshal.TryRead(guidArray, out Guid readGuid))
            {
                readGuid = new Guid(guidArray.ToArray());
            }
            return readGuid;
#endif
        }

        static readonly long BaseDateTicks = new DateTime(1900, 1, 1).Ticks;
    }
}