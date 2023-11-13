namespace NServiceBus;

using System;

static class PathUtilities
{
    public static string SanitizedPath(string commandLine)
    {
        if (commandLine.StartsWith('"'))
        {
            var nextIndex = commandLine.IndexOf('"', 1);
            if (nextIndex == -1)
            {
                throw new FormatException("The provided path is in an invalid format");
            }

            return commandLine[1..nextIndex];
        }

        var firstSpace = commandLine.IndexOf(' ');
        if (firstSpace == -1)
        {
            return commandLine;
        }

        return commandLine[..firstSpace];
    }
}