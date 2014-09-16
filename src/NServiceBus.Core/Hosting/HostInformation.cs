namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Utils;

    /// <summary>
    /// Provides a information about the process hosting this endpoint
    /// </summary>
    public class HostInformation
    {
        internal static HostInformation CreateDefault()
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

            var hostInfo= new HostInformation(hostId, machineName);

            hostInfo.Properties.Add("PathToExecutable", fullPathToStartingExe);

            return hostInfo;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="hostId">The id of the host</param>
        /// <param name="displayName">The display name of the host</param>
        public HostInformation(Guid hostId, string displayName)
        {
            HostId = hostId;
            DisplayName = displayName;

            Properties = new Dictionary<string, string>
            {
                {"Machine", Environment.MachineName},
                {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                {"UserName", Environment.UserName},
            };
        }

        /// <summary>
        /// The unique identifier for this host
        /// </summary>
        public Guid HostId { get; private set; }

        /// <summary>
        /// The display name of this host
        /// </summary>
        public string DisplayName { get; private set; }
        
        /// <summary>
        /// A set of properties for the host. This might vary from host to host
        /// </summary>
        public Dictionary<string, string> Properties { get; private set; }
    }
}