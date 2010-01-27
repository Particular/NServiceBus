using System;
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
        public void Configure(IConfigureThisEndpoint specifier)
        {
            Console.WriteLine("I'm going to do my custom logging setup in here using my own profile.");
        }
    }
}
