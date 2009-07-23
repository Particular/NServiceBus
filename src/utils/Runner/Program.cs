using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            NServiceBus.Utils.MsmqInstallation.Install();

            Console.ReadLine();
        }
    }
}
