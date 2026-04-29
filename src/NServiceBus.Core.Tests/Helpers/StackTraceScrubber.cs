#nullable enable

namespace NServiceBus.Core.Tests.Helpers;

using System;
using System.Text.RegularExpressions;

public static partial class StackTraceScrubber
{
    public static string ScrubFileInfoFromStackTrace(string value)
    {
        var scrubbedPaths = StackTracePathRegex().Replace(value, static match =>
        {
            var path = match.Groups["path"].Value;
            var parts = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

            return parts.Length switch
            {
                >= 2 => $"{parts[^2]}/{parts[^1]}",
                1 => parts[0],
                _ => string.Empty
            };
        });

        return StackTraceLineInfoRegex().Replace(scrubbedPaths, string.Empty);
    }

    [GeneratedRegex(@"(?<path>(?:[A-Za-z]:)?(?:[/\\][^:/\\\r\n]+)+\.cs)")]
    private static partial Regex StackTracePathRegex();

    [GeneratedRegex(@"(?<=\.cs):\p{L}+\s+\d+")]
    private static partial Regex StackTraceLineInfoRegex();
}