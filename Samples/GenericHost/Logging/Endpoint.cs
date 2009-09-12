using System;
using NServiceBus.Host;
using NServiceBus;

namespace Logging
{
    /*  In this sample, we want our own production logging while leaving the regular NServiceBus
        configuration of the endpoint so we specify "Logging.MyProductionProfile" on the command line.
     */

    public class Endpoint : IConfigureThisEndpoint {}

    public class MyProductionProfile : Production {}

    public class MyProductionLogging : IConfigureLoggingForProfile<MyProductionProfile>
    {
        public void ConfigureLogging()
        {
            Console.WriteLine("I'm configuring logging now.");
        }
    }
}
