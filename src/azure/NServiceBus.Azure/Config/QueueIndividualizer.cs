namespace NServiceBus.Config
{
    using System.Globalization;
    using Microsoft.WindowsAzure.ServiceRuntime;

    internal class QueueIndividualizer
    {
        public static string Individualize(string queueName)
        {
            var individualQueueName = queueName;
            if (RoleEnvironment.IsAvailable)
            {
                var index = ParseIndexFrom(RoleEnvironment.CurrentRoleInstance.Id);
                individualQueueName = ParseQueueNameFrom(queueName)
                                          + (index > 0 ? "-" : "")
                                          + (index > 0 ? index.ToString(CultureInfo.InvariantCulture) : "");

                if (queueName.Contains("@"))
                    individualQueueName += "@" + ParseMachineNameFrom(queueName);
            }

            return individualQueueName;
        }

        private static string ParseMachineNameFrom(string inputQueue)
        {
            return inputQueue.Contains("@") ? inputQueue.Substring(inputQueue.IndexOf("@", System.StringComparison.Ordinal) + 1) : string.Empty;
        }

        private static object ParseQueueNameFrom(string inputQueue)
        {
            return inputQueue.Contains("@") ? inputQueue.Substring(0, inputQueue.IndexOf("@", System.StringComparison.Ordinal)) : inputQueue;
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