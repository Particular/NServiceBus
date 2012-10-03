namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Msmq;

    [Cmdlet(VerbsLifecycle.Install, "Msmq")]
    public class InstallMsmq : PSCmdlet
    {

        [Parameter(HelpMessage = "Checks if msmq is good without fixing any potential issues")]
        public SwitchParameter WhatIf { get; set; }

        [Parameter(HelpMessage = "Path to install RavenDB into, default is %ProgramFiles%\\NServiceBus.Persistence")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            bool msmqIsGood;
            if (WhatIf)
            {
                msmqIsGood = MsmqSetup.IsInstallationGood();
                Console.Out.WriteLine(msmqIsGood
                                          ? "Msmq is installed and setup for use with NServiceBus"
                                          : "Msmq is not installed, if you rerun the command without -WhatIf Msmq will be reinstalled automatically for you");

                WriteObject(msmqIsGood);
                return;
            }

            msmqIsGood = MsmqSetup.StartMsmqIfNecessary(Force);

            if (!msmqIsGood && !Force)
                Console.Out.WriteLine("Msmq needs to reinstalled, Please rerun the command with -Force set. NOTE: This will remove all local queues!");

            WriteObject(msmqIsGood);
        }
    }
}
