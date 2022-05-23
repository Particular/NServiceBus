namespace NServiceBus.MessageDrivePubSub.Compatibility.Cli
{
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main()
        {
            //1. Upgrade message-driven pub-sub endpoint by:
            //   a. Converting all persistence subscription entries to topology changes
            //   b. Setting up own subscriptions by running installers (for native publishers)
            //   c. Setting up own subscriptions by running cli (for message-driven publishers)

            //TODO: spike converting subscription table entries to the native topology setup 
            //Spike scenario: start with message-driven pub-sub sample, migrate the publisher to native pub-sub

            var migratorEndpoint = new EndpointConfiguration("MigratorEndpoint");
            migratorEndpoint.SendOnly();

            migratorEndpoint.


            //2. Introduce native pub-sub to the system by:
            //   a. Generating Subscribe messages via cli to setup own subscriptions (native setup handled by installers)
            //   b. Apply native topology changes via cli to setup external subscriptions

            //TODo: spike generating pub-sub messages and pub-sub topology setup via cli

        }
    }
}