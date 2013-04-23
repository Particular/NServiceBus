namespace NServiceBus.Wix.CustomActions
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;
    using PowerShell;
    using Setup.Windows.Dtc;
    using Setup.Windows.Msmq;
    using Setup.Windows.PerformanceCounters;
    using Setup.Windows.RavenDB;

    public class CustomActions
    {
        [CustomAction]
        public static ActionResult InstallMsmq(Session session)
        {
            using (var msgRec = new Record("InstallMsmq", "Installing and starting MSMQ if necessary...", String.Empty))
            {
                session.Message(InstallMessage.ActionStart, msgRec);
            }

            try
            {
                CaptureOut(() =>
                    {
                        if (MsmqSetup.StartMsmqIfNecessary(true))
                        {
                            using (var msgRec = new Record("InstallMsmq", "MSMQ installed and configured", String.Empty))
                            {
                                session.Message(InstallMessage.ActionStart, msgRec);
                            }
                        }
                        else
                        {
                            using (var msgRec = new Record("InstallMsmq", "MSMQ already properly configured", String.Empty))
                            {
                                session.Message(InstallMessage.ActionStart, msgRec);
                            }
                        }

                        session["MSMQ_INSTALL"] = "SUCCESS";
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
            using (var msgRec = new Record("InstallDtc", "Installing and starting DTC if necessary...", String.Empty))
            {
                session.Message(InstallMessage.ActionStart, msgRec);
            }

            try
            {
                CaptureOut(() =>
                    {
                        DtcSetup.StartDtcIfNecessary();
                        session["DTC_INSTALL"] = "SUCCESS";

                        using (var msgRec = new Record("InstallDtc", "DTC installed and configured", String.Empty))
                        {
                            session.Message(InstallMessage.ActionStart, msgRec);
                        }

                    }, session);

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("InstallDtc failed: {0}", ex);

                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult InstallRavenDb(Session session)
        {
            using (var msgRec = new Record("InstallRavenDb", "Installing RavenDB if necessary...", String.Empty))
            {
                session.Message(InstallMessage.ActionStart, msgRec);
            }

            try
            {
                int port;

                if (!int.TryParse(session["RAVEN_PORT"], out port))
                {
                    session.Log("No RAVEN_PORT property found please set it.");

                    return ActionResult.Failure;
                }

                string installPath = session["RAVEN_INSTALLPATH"];
                   
                CaptureOut(() =>
                    {
                        RavenDBSetup.Install(port, installPath);

                        session["RAVEN_INSTALL"] = "SUCCESS";

                        using (var msgRec = new Record("InstallRavenDb", "RavenDB installed and configured", String.Empty))
                        {
                            session.Message(InstallMessage.ActionStart, msgRec);
                        }
                    }, session);


                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("InstallRavenDb failed: {0}", ex);

                return ActionResult.Failure;
            }
        }


        [CustomAction]
        public static ActionResult DetectRavenDBPort(Session session)
        {
            using (var msgRec = new Record("DetectRavenDBPort", "Detecting RavenDB port...", String.Empty))
            {
                session.Message(InstallMessage.ActionStart, msgRec);
                session.Message(InstallMessage.Info, msgRec);
            }

            try
            {
                CaptureOut(() =>
                    {
                        var port = RavenDBSetup.FindRavenDBPort();

                        if (port != 0)
                        {
                            session["RAVEN_ISINSTALLED"] = "true";
                            session["RAVEN_PORT"] = port.ToString(CultureInfo.InvariantCulture);

                        }
                        else
                        {
                            session["RAVEN_ISINSTALLED"] = "false";
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
        public static ActionResult FindAvailablePort(Session session)
        {
            using (var msgRec = new Record("FindAvailablePort", "Finding an available port where RavenDB can be installed...", String.Empty))
            {
                session.Message(InstallMessage.ActionStart, msgRec);
            }
            
            try
            {
                CaptureOut(() =>
                {
                        session["PORT_AVAILABLE"] = PortUtils.FindAvailablePort(8080).ToString(CultureInfo.InvariantCulture);
                    
                }, session);

                return ActionResult.Success;
            }
            catch (Exception)
            {
                return ActionResult.Failure;
            }
        }


        [CustomAction]
        public static ActionResult IsPortAvailable(Session session)
        {
            try
            {
                int port;

                if (!int.TryParse(session["PORT_TOCHECK"], out port))
                {
                    session.Log("No PORT_TOCHECK property found please set it.");

                    return ActionResult.Failure;
                }

                using (var msgRec = new Record("IsPortAvailable", string.Format("Checking if port {0} is available", port), String.Empty))
                {
                    session.Message(InstallMessage.ActionStart, msgRec);
                }


                CaptureOut(() =>
                    {
                        session["PORT_CHECK"] = PortUtils.IsPortAvailable(port).ToString();

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
            using (var msgRec = new Record("InstallPerformanceCounters", "Installing NSB performance counters...", String.Empty))
            {
                session.Message(InstallMessage.ActionStart, msgRec);
            }

            try
            {
                CaptureOut(() =>
                    {
                        PerformanceCounterSetup.SetupCounters();

                        session["COUNTERS_INSTALL"] = "SUCCESS";

                        using (var msgRec = new Record("InstallPerformanceCounters", "NSB performance counters installed", String.Empty))
                        {
                            session.Message(InstallMessage.ActionStart, msgRec);
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