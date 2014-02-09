namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using Utils;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HostInformation
    {
        public static HostInformation CreateDefault()
        {
            var commandLine = Environment.CommandLine;

            var fullPathToStartingExe = commandLine.Split('"')[1];

            var hostId = DeterministicGuid.Create(fullPathToStartingExe, Environment.MachineName);

            return new HostInformation(hostId, Environment.MachineName, String.Format("{0}", fullPathToStartingExe));
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