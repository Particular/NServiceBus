namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Utils;

    public class HostInformation
    {
        readonly Guid hostId;
        readonly string displayName;

        public HostInformation()
        {
            Properties = new Dictionary<string, string>
            {
                {"Machine", Environment.MachineName},
                {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                {"UserName", Environment.UserName},
                {"CommandLine", Environment.CommandLine}
            };
        }

        public static HostInformation CreateDefault()
        {

            var commandLine = Environment.CommandLine;

            var fullPathToStartingExe = commandLine.Split('"')[1];

            var hostId = DeterministicGuid.Create(fullPathToStartingExe, Environment.MachineName);

            return new HostInformation(hostId, String.Format("{0}", fullPathToStartingExe));
        }

        public HostInformation(Guid hostId, string displayName)
        {
            this.hostId = hostId;
            this.displayName = displayName;
        }

        public Guid HostId
        {
            get { return hostId; }
        }

        public string DisplayName
        {
            get { return displayName; }
        }

        public Dictionary<string, string> Properties { get; set; }
    }
}