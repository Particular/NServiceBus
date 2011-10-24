namespace NServiceBus.Unicast.Subscriptions.Raven
{
    public class Subscription
    {
        public string Id { get; set; }

        public MessageType MessageType { get; set; }

        public Address Client { get; set; }

        public static string FormatId(string endpoint, MessageType messageType, string client)
        {
            return string.Format("Subscriptions/{0}/{1}/{2}", endpoint, messageType, client);
        }
    }
}