namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using Support;

    public static class Transports
    {

        public static RunDescriptor ActiveMQ = new RunDescriptor
            {
                Key = "ActiveMQ",
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
                Key = "Msmq",
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
            Key = "RabbitMQ",
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
            Key = "SqlServer",
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