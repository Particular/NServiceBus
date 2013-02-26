namespace NServiceBus.Transports.ActiveMQ
{
    using Apache.NMS;
    using Apache.NMS.ActiveMQ.Commands;
    using Apache.NMS.Util;

    public class DestinationEvaluator : IDestinationEvaluator
    {
        private static string AddPrefix(string destination, string destinationPrefix)
        {
            return destination.StartsWith(destinationPrefix) || (destination.StartsWith("temp-" + destinationPrefix))
                       ? destination
                       : destinationPrefix + destination;
        }

        public IDestination GetDestination(ISession session, string destination, string prefix)
        {
            destination = AddPrefix(destination, prefix);

            if (destination.StartsWith("temp-queue://"))
            {
                return new ActiveMQTempQueue(destination.Substring("temp-queue://".Length));
            }

            if (destination.StartsWith("temp-topic://"))
            {
                return new ActiveMQTempTopic(destination.Substring("temp-topic://".Length));
            }

            return SessionUtil.GetDestination(session, destination);
        }
    }
}