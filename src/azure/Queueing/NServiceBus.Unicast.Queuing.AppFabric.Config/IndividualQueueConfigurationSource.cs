using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config.ConfigurationSource;

namespace NServiceBus.Config
{
    internal class IndividualQueueConfigurationSource : IConfigurationSource
    {
        private readonly IConfigurationSource innerSource;

        public IndividualQueueConfigurationSource(IConfigurationSource innerSource)
        {
            this.innerSource = innerSource;
        }

        T IConfigurationSource.GetConfiguration<T>()
        {
            var config = innerSource.GetConfiguration<T>();
            var index = 0;
            if (RoleEnvironment.IsAvailable)
                index = ParseIndexFrom(RoleEnvironment.CurrentRoleInstance.Id);

            var queueConfig = config as AzureServiceBusQueueConfig;
            if (queueConfig != null && queueConfig.QueueName != null && RoleEnvironment.IsAvailable)
            {
                var individualQueueName = ParseQueueNameFrom(queueConfig.QueueName)
                                          + (index > 0 ? "-" : "")
                                          + (index > 0 ? index.ToString() : "");

                if (queueConfig.QueueName.Contains("@"))
                    individualQueueName += "@" + ParseMachineNameFrom(queueConfig.QueueName);

                queueConfig.QueueName = individualQueueName;
            }
            
            return config;
        }

        private string ParseMachineNameFrom(string inputQueue)
        {
            return inputQueue.Contains("@") ? inputQueue.Substring(inputQueue.IndexOf("@") + 1) : string.Empty;
        }

        private object ParseQueueNameFrom(string inputQueue)
        {
            return inputQueue.Contains("@") ? inputQueue.Substring(0, inputQueue.IndexOf("@")) : inputQueue;
        }

        private static int ParseIndexFrom(string id)
        {
            var idArray = id.Split('.');
            int index;
            if (!int.TryParse((idArray[idArray.Length - 1]), out index))
            {
                idArray = id.Split('_');
                index = int.Parse((idArray[idArray.Length - 1]));
            }
            return index;
        }
    }
}