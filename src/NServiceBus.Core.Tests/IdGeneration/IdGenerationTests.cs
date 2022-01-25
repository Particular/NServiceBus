namespace NServiceBus.Core.Tests.IdGeneration
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using NUnit.Framework;
#if NETFRAMEWORK
    using System.Runtime.InteropServices;
#endif

    [TestFixture]
    public class IdGenerationTests
    {
        [TestCaseSource(nameof(RandomizedInput))]
        public void Should_return_same_result_for_same_input(Guid guid, DateTime now)
        {
            var oldCombGuid = LocalCopyOfCombGuid.GenerateOld(guid, now);
            var improvedCombGuid = LocalCopyOfCombGuid.GenerateImproved(guid, now);

            Assert.AreEqual(oldCombGuid, improvedCombGuid);
        }

        static readonly Random Random = new Random();

        static IEnumerable<object[]> RandomizedInput
        {
            get
            {
                for (int i = 0; i < 20; i++)
                {
                    yield return new object[]
                    {
                        Guid.NewGuid(),
                        DateTime.UtcNow.AddDays(Random.Next(0, 30)).AddHours(Random.Next(0, 24)).AddMinutes(Random.Next(0, 60)).AddSeconds(Random.Next(0, 60))
                    };
                }
            }
        }

        class LocalCopyOfCombGuid
        {
            // Only slightly modified copy of the original algorithm
            public static Guid GenerateOld(Guid input, DateTime nowInput)
            {
                var guidArray = input.ToByteArray();

                var now = nowInput; // Internal use, no need for DateTimeOffset

                // Get the days and milliseconds which will be used to build the byte string
                var days = new TimeSpan(now.Ticks - BaseDateTicks);
                var timeOfDay = now.TimeOfDay;

                // Convert to a byte array
                // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
                var daysArray = BitConverter.GetBytes(days.Days);
                var millisecondArray = BitConverter.GetBytes((long)(timeOfDay.TotalMilliseconds / 3.333333));

                // Reverse the bytes to match SQL Servers ordering
                Array.Reverse(daysArray);
                Array.Reverse(millisecondArray);

                // Copy the bytes into the guid
                Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
                Array.Copy(millisecondArray, millisecondArray.Length - 4, guidArray, guidArray.Length - 4, 4);

                return new Guid(guidArray);
            }

            public static Guid GenerateImproved(Guid input, DateTime nowInput)
            {
                var newGuid = input;
                Span<byte> guidArray = stackalloc byte[16];
#if NETCOREAPP
                if (!newGuid.TryWriteBytes(guidArray))
#elif NETFRAMEWORK
                if (!MemoryMarshal.TryWrite(guidArray, ref newGuid))
#endif
                {

                    guidArray = newGuid.ToByteArray();
                }

                var now = nowInput; // Internal use, no need for DateTimeOffset

                // Get the days and milliseconds which will be used to build the byte string
                var days = new TimeSpan(now.Ticks - BaseDateTicks);
                var timeOfDay = now.TimeOfDay;

                // Convert to a byte array
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
}