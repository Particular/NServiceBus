using System;

namespace Runner
{
    class Program
    {
        static void Main()
        {
            NServiceBus.Utils.MsmqInstallation.InstallMsmqIfNecessary();
            Console.ReadLine();
        }
    }
}
