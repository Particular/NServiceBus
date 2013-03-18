namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using Microsoft.Win32;

    [Cmdlet(VerbsCommon.Get, "NServiceBusLocalMachineSettings")]
    public class GetNServiceBusLocalMachineSettings : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            using (var registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                if (registryKey == null)
                {
                    WriteObject(new
                        {
                            ErrorQueue = (string) null,
                            AuditQueue = (string) null,
                        });
                    return;
                }

                WriteObject(new
                    {
                        ErrorQueue = (string) registryKey.GetValue("ErrorQueue"),
                        AuditQueue = (string) registryKey.GetValue("AuditQueue"),
                    });
            }
        }
    }
}