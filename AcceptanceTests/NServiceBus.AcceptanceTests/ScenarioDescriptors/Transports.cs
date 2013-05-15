namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using AcceptanceTesting.Support;

    public static class Transports
    {
        public static RunDescriptor Default
        {
            get { return Msmq; }
        }

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

        public static readonly RunDescriptor AzureServiceBus = new RunDescriptor
        {
            Key = "AzureServiceBus",
            Settings =
                new Dictionary<string, string>
                                {
                                    {
                                        "Transport",
                                        typeof(AzureServiceBus).AssemblyQualifiedName
                                    }
                                }
        };
        public static readonly RunDescriptor AzureStorageQueue = new RunDescriptor
        {
            Key = "AzureStorageQueue",
            Settings =
                new Dictionary<string, string>
                                {
                                    {
                                        "Transport",
                                        typeof(AzureStorageQueue).AssemblyQualifiedName
                                    }
                                }
        };  

        
    }
}