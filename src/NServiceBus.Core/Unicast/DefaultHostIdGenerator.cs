namespace NServiceBus.Unicast
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NServiceBus.Utils;

    struct DefaultHostIdGenerator
    {
        public string FullPathToStartingExe;
        public Guid HostId;

        public DefaultHostIdGenerator(string commandLine, string machineName)
        {
            if (commandLine.StartsWith("\""))
            {
                FullPathToStartingExe = (from Match match in Regex.Matches(commandLine, "\"([^\"]*)\"")
                    select match.ToString()).First().Trim('"');
            }
            else
            {
                FullPathToStartingExe = commandLine.Split(' ').First();
            }

            HostId = DeterministicGuid.Create(FullPathToStartingExe, machineName);
        }
    }
}