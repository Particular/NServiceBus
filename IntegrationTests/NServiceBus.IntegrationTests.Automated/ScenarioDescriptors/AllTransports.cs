namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using Support;

    public class AllTransports : ScenarioDescriptor
    {
        public AllTransports()
        {
            this.Add(
                new RunDescriptor
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
                    });

            this.Add(
                new RunDescriptor
                    {
                        Name = "ActiveMQ",
                        Settings =
                            new Dictionary<string, string>
                                {
                                    {
                                        "Transport",
                                        typeof(ActiveMQ).AssemblyQualifiedName
                                    }
                                }
                    });

            this.Add(
              new RunDescriptor
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
              });

            this.Add(
              new RunDescriptor
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
              });
        }
    }
}