namespace NServiceBus;

using System;

static class DateTimeOffsetExtensions
{
    public static int Microseconds(this DateTimeOffset self) => (int)Math.Floor(self.Ticks % TimeSpan.TicksPerMillisecond / (double)ticksPerMicrosecond);

    public static DateTimeOffset AddMicroseconds(this DateTimeOffset self, int microseconds) => self.AddTicks(microseconds * ticksPerMicrosecond);

    const int ticksPerMicrosecond = (int)TimeSpan.TicksPerMillisecond / 1000;
}