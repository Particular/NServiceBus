using System;

namespace Runner
{
    class Program
    {

        //TODO: Remove this project and add a unittest project that does this in a test marked as "Explicit" instead
        static void Main()
        {
            NServiceBus.Utils.MsmqInstallation.StartMsmqIfNecessary();
            Console.ReadLine();
        }
    }
}
