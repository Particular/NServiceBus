namespace NServiceBus.Persistence.Raven.Installation
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Security.Principal;

    /// <summary>
    /// Copied from RavenDB and modified for out needs
    /// </summary>
    public static class RavenHelpers
    {
        public static bool EnsureCanListenToWhenInNonAdminContext(int port)
        {
            try
            {
                if (CanStartHttpListener(port))
                    return true;

               TryGrantingHttpPrivileges(port);

               return CanStartHttpListener(port);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void GetArgsForHttpAclCmd(int port, out string args, out string cmd)
        {
            if (Environment.OSVersion.Version.Major > 5)
            {
                cmd = "netsh";
                args = string.Format(@"http add urlacl url=http://+:{0}/ user=""{1}""", port,
                                     WindowsIdentity.GetCurrent().Name);
            }
            else
            {
                cmd = "httpcfg";
                args = string.Format(@"set urlacl /u http://+:{0}/ /a D:(A;;GX;;;""{1}"")", port,
                                     WindowsIdentity.GetCurrent().User);
            }
        }

        private static bool CanStartHttpListener(int port)
        {
            try
            {
                var httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://+:" + port + "/");
                httpListener.Start();
                httpListener.Stop();
                
                return true;
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode != 5) //access denies
                    throw;
            }

            return false;
        }

        private static void TryGrantingHttpPrivileges(int port)
        {
            string args;
            string cmd;
            GetArgsForHttpAclCmd(port, out args, out cmd);

            Console.WriteLine("Trying to grant rights for http.sys");
            try
            {
                Console.WriteLine("runas {0} {1}", cmd, args);
                var process = Process.Start(new ProcessStartInfo
                {
                    Verb = "runas",
                    Arguments = args,
                    FileName = cmd,
                });
                process.WaitForExit();
            }
            catch
            {
            }
        }
    }

}