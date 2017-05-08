namespace NServiceBus
{
    using System;
    using System.IO;

    static class PathChecker
    {
        public static void ThrowForBadPath(string value, string valueName)
        {
            var invalidPathChars = Path.GetInvalidPathChars();

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.IndexOfAny(invalidPathChars) < 0)
            {
                return;
            }

            throw new Exception($"The value for '{valueName}' has illegal path characters. Provided value: {value}. Must not contain any of {string.Join(", ", invalidPathChars)}.");
        }
    }
}