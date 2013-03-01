namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
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

            using (var registryKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\NServiceBus"))
            {
                if (registryKey == null)
                {
                    Host.UI.WriteErrorLine(@"Could not create/open 'HKEY_LOCAL_MACHINE\SOFTWARE\NServiceBus' for writing.");
                    return;
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