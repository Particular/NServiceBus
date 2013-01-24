namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using Support;

    public static class Transports
    {
        public static readonly RunDescriptor ActiveMQ = new RunDescriptor
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


        public static readonly RunDescriptor Msmq = new RunDescriptor
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



        public static readonly RunDescriptor RabbitMQ = new RunDescriptor
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


        public static readonly RunDescriptor SqlServer = new RunDescriptor
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