using System;

namespace Runner
{
    class Program
    {
        static void Main()
        {
            NServiceBus.Utils.MsmqInstallation.StartMsmqIfNecessary();
            Console.ReadLine();
        }
    }
}
