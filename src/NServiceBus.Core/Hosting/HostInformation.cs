namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Settings;
    using Support;
    using Utils;

    public class HostInformation
    {
        public HostInformation()
        {
            Properties = new Dictionary<string, string>
            {
                {"Machine", Environment.MachineName},
                {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                {"UserName", Environment.UserName}, //check this
                {"CommandLine", Environment.CommandLine}
            };
        }

        public Guid HostId { get; set; }

        public string DisplayName { get; set; }

        public Dictionary<string, string> Properties { get; set; }
    }

    class DefaultHostInformation
    {
        public static HostInformation GetCurrent()
        {
            var commandLine = Environment.CommandLine;

            var fullPathToStartingExe = commandLine.Split('"')[1];

            return new HostInformation
            {
                HostId = DeterministicGuid.Create(fullPathToStartingExe,Environment.MachineName), //todo is this what we need?
                DisplayName = string.Format("{0}", fullPathToStartingExe)
               
            };
        }
    }
    class Defaults : ISetDefaultSettings
    {
        public Defaults()
        {
            SettingsHolder.SetDefault<HostInformation>(DefaultHostInformation.GetCurrent());
        }
    }

    class RegisterHostInformation:IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            var hostInformation = SettingsHolder.Get<HostInformation>(typeof(HostInformation).FullName);

            Configure.Component(() => hostInformation, DependencyLifecycle.SingleInstance);
        }
    }

    
}