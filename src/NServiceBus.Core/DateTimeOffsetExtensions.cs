namespace NServiceBus
{
    using System;

    static class DateTimeOffsetExtensions
    {
        public static int Microseconds(this DateTimeOffset self)
        {
            // This appears to be a false positive for IDE0047
#pragma warning disable IDE0047 // Remove unnecessary parentheses
            return (int)Math.Floor((self.Ticks % TimeSpan.TicksPerMillisecond) / (double)ticksPerMicrosecond);
#pragma warning restore IDE0047 // Remove unnecessary parentheses
        }

        public static DateTimeOffset AddMicroseconds(this DateTimeOffset self, int microseconds)
        {
            return self.AddTicks(microseconds * ticksPerMicrosecond);
        }

        const int ticksPerMicrosecond = (int)TimeSpan.TicksPerMillisecond / 1000;
    }
}