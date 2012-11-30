namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using System.Reflection;

    [Cmdlet(VerbsCommon.Get, "NServiceBusVersion")]
    public class GetNServiceBusVersion : PSCmdlet
    {
        protected override void ProcessRecord()
        {

            var appName = Assembly.GetAssembly(typeof(GetNServiceBusVersion)).Location;
            var assembyVersion = AssemblyName.GetAssemblyName(appName).Version;

            //build a semver compliant version
            var version = new Version(assembyVersion.Major, assembyVersion.Minor,assembyVersion.Build);


            WriteObject(version);
        }
    }

}