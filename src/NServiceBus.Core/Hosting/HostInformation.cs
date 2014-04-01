namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Utils;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HostInformation
    {
        public static HostInformation CreateDefault()
        {
            var commandLine = Environment.CommandLine;

            return CreateHostInformation(commandLine, Environment.MachineName);
        }

        internal static HostInformation CreateHostInformation(string commandLine, string machineName)
        {
            string fullPathToStartingExe;

            if (commandLine.StartsWith("\""))
            {
                fullPathToStartingExe = (from Match match in Regex.Matches(commandLine, "\"([^\"]*)\"")
                    select match.ToString()).First().Trim('"');
            }
            else
            {
                fullPathToStartingExe = commandLine.Split(' ').First();
            }

            var hostId = DeterministicGuid.Create(fullPathToStartingExe, machineName);

            return new HostInformation(hostId, machineName, String.Format("{0}", fullPathToStartingExe));
        }

        public HostInformation(Guid hostId, string displayName, string displayInstanceIdentifier)
        {
            HostId = hostId;
            DisplayName = displayName;
            DisplayInstanceIdentifier = displayInstanceIdentifier;

            Properties = new Dictionary<string, string>
            {
                {"Machine", Environment.MachineName},
                {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                {"UserName", Environment.UserName},
            };
        }

        public Guid HostId { get; private set; }
        public string DisplayName { get; private set; }
        public string DisplayInstanceIdentifier { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }
    }
}