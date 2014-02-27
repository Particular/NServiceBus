﻿namespace NServiceBus.Licensing
{
    using System;
    using System.Globalization;

    static class UniversalDateParser
    {
        public static DateTime Parse(string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal).ToUniversalTime();
        }
    }
}