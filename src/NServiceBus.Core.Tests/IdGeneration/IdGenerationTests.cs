namespace NServiceBus.Core.Tests.IdGeneration
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class IdGenerationTests
    {
        [TestCaseSource(nameof(RandomizedInput))]
        public void Should_return_same_result_for_same_input(Guid guid, DateTime now)
        {
            var oldCombGuid = LocalCopyOfCombGuid.GenerateOld(guid, now);
            var improvedCombGuid = CombGuid.Generate(guid, now);

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

            static readonly long BaseDateTicks = new DateTime(1900, 1, 1).Ticks;
        }
    }
}