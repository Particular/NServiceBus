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
            var assemblyVersion = AssemblyName.GetAssemblyName(appName).Version;

            //build a semver compliant version
            var version = new Version(assemblyVersion.Major, assemblyVersion.Minor,assemblyVersion.Build);


            WriteObject(version);
        }
    }

}