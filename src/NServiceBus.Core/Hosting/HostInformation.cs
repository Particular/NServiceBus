namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Utils;

    public class HostInformation
    {
        public static HostInformation CreateDefault()
        {
            var commandLine = Environment.CommandLine;

            var fullPathToStartingExe = commandLine.Split('"')[1];

            var hostId = DeterministicGuid.Create(fullPathToStartingExe, Environment.MachineName);

            return new HostInformation(hostId, String.Format("{0}", fullPathToStartingExe));
        }

        public HostInformation(Guid hostId, string displayName)
        {
            HostId = hostId;
            DisplayName = displayName;

            Properties = new Dictionary<string, string>
            {
                {"Machine", Environment.MachineName},
                {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                {"UserName", Environment.UserName},
                {"CommandLine", Environment.CommandLine}
            };
        }

        public Guid HostId { get; private set; }

        public string DisplayName { get; private set; }

        public Dictionary<string, string> Properties { get; private set; }
    }
}