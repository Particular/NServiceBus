namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using System.Security;
    using Microsoft.Win32;

    [Cmdlet(VerbsCommon.Set, "NServiceBusLocalMachineSettings", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class SetNServiceBusLocalMachineSettings : PSCmdlet
    {
        [Parameter(Mandatory = false, HelpMessage = "Error queue to use for all endpoints in this machine.")]
        public string ErrorQueue { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Audit queue to use for all endpoints in this machine.")]
        public string AuditQueue { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Environment.MachineName))
            {
                return;
            }

            if (Environment.Is64BitOperatingSystem)
            {
                WriteRegistry(RegistryView.Registry32);

                WriteRegistry(RegistryView.Registry64);
            }
            else
            {
                WriteRegistry(RegistryView.Default);
            }
        }

        void WriteRegistry(RegistryView view)
        {
            using (var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).CreateSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                if (registryKey == null)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new SecurityException(
                                @"Could not create/open 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus' for writing."),
                            "NotAuthorized", ErrorCategory.SecurityError, null));
                }

                if (!String.IsNullOrWhiteSpace(ErrorQueue))
                {
                    registryKey.SetValue("ErrorQueue", ErrorQueue, RegistryValueKind.String);
                }

                if (!String.IsNullOrWhiteSpace(AuditQueue))
                {
                    registryKey.SetValue("AuditQueue", AuditQueue, RegistryValueKind.String);
                }
            }
        }
    }
}