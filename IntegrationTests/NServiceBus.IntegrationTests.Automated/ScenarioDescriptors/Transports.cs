namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using Support;

    public static class Transports
    {

        public static RunDescriptor ActiveMQ = new RunDescriptor
            {
                Name = "ActiveMQ",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Transport",
                                typeof (ActiveMQ).AssemblyQualifiedName
                            }
                        }
            };


        public static RunDescriptor Msmq = new RunDescriptor
            {
                Name = "Msmq",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Transport",
                                typeof(Msmq).AssemblyQualifiedName
                            }
                        }
            };



        public static RunDescriptor RabbitMQ = new RunDescriptor
        {
            Name = "RabbitMQ",
            Settings =
                new Dictionary<string, string>
                                {
                                    {
                                        "Transport",
                                        typeof(RabbitMQ).AssemblyQualifiedName
                                    }
                                }
        };


        public static RunDescriptor SqlServer = new RunDescriptor
        {
            Name = "SqlServer",
            Settings =
                new Dictionary<string, string>
                                {
                                    {
                                        "Transport",
                                        typeof(SqlServer).AssemblyQualifiedName
                                    }
                                }
        };  
    }
}