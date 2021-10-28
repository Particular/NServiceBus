namespace NServiceBus
{
    using System;
    using System.IO;


    /// <summary>
    /// TODO (should this be moved to a different namespace)
    /// Helper for file path verification.
    /// </summary>
    public static class PathChecker
    {
        /// <summary>
        /// Checks a string for invalid file path characters.
        /// </summary>
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