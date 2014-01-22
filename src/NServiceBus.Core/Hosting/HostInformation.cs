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
                HostId = DeterministicGuid.Create(fullPathToStartingExe), //todo is this what we need?
                DisplayName = string.Format("{0}@{1}", fullPathToStartingExe, RuntimeEnvironment.MachineName),
                Properties = new Dictionary<string, string>
                {
                   {"Machine", RuntimeEnvironment.MachineName},
                   {"ProcessID",Process.GetCurrentProcess().Id.ToString()},
                   {"WorkingDirectory",Environment.CurrentDirectory},
                   {"UserName",Environment.UserName},
                   {"CommandLine",Environment.CommandLine},
                   {"InDebugMode",Debugger.IsAttached.ToString()},

                }
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

            Configure.Component(() => hostInformation, DependencyLifecycle.InstancePerCall);
        }
    }

    
}