namespace NServiceBus.Wix.CustomActions
{
    using System;
    using System.IO;
    using System.Security.Principal;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;
    using Setup.Windows.Dtc;
    using Setup.Windows.Msmq;
    using Setup.Windows.PerformanceCounters;
    using Setup.Windows.RavenDB;

    public class CustomActions
    {
        [CustomAction]
        public static ActionResult InstallMsmq(Session session)
        {
            session.Log("Installing/Starting MSMQ if necessary.");

            try
            {
                CaptureOut(() =>
                    {
                        if (MsmqSetup.StartMsmqIfNecessary(true))
                        {
                            session.Log("MSMQ installed and configured.");
                        }
                        else
                        {
                            session.Log("MSMQ already properly configured.");
                        }
                    }, session);

                return ActionResult.Success;
            }
            catch (Exception)
            {
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult InstallDtc(Session session)
        {
            session.Log("Installing/Starting DTC if necessary.");

            try
            {
                CaptureOut(() =>
                    {
                        if (DtcSetup.StartDtcIfNecessary(true))
                        {
                            session.Log("DTC installed and configured.");
                        }
                        else
                        {
                            session.Log("DTC already properly configured.");
                        }
                    }, session);

                return ActionResult.Success;
            }
            catch (Exception)
            {
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult InstallRavenDb(Session session)
        {
            session.Log("Installing RavenDB if necessary.");

            try
            {
                CaptureOut(() =>
                    {
                        var ravenDbSetup = new RavenDBSetup();
                        if (ravenDbSetup.Install(WindowsIdentity.GetCurrent(), allowInstall: true))
                        {
                            session.Log("RavenDB installed and configured.");
                        }
                        else
                        {
                            session.Log("RavenDB could not be installed.");
                        }
                    }, session);

                return ActionResult.Success;
            }
            catch (Exception)
            {
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult InstallPerformanceCounters(Session session)
        {
            session.Log("Installing NSB performance counters.");

            try
            {
                CaptureOut(() =>
                    {
                        if (PerformanceCounterSetup.SetupCounters(true))
                        {
                            session.Log("NSB performance counters installed.");
                        }
                        else
                        {
                            session.Log("NSB performance counters already installed.");
                        }
                    }, session);

                return ActionResult.Success;
            }
            catch (Exception)
            {
                return ActionResult.Failure;
            }
        }

        private static void CaptureOut(Action execute, Session session)
        {
            var sb = new StringBuilder();
            TextWriter standardOut = Console.Out;
            using (var stringWriter = new StringWriter(sb))
            {
                Console.SetOut(stringWriter);

                try
                {
                    execute();

                    session.Log(sb.ToString());
                }
                finally
                {
                    Console.SetOut(standardOut);
                }
            }
        }
    }
}