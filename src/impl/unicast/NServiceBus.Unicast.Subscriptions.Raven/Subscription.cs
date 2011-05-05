namespace NServiceBus.Unicast.Subscriptions.Raven
{
    public class Subscription
    {
        public string Id { get; set; }

        public string MessageType { get; set; }

        public string Client { get; set; }

        public static string FormatId(string endpoint, string messageType, string client)
        {
            return string.Format("Subscriptions/{0}/{1}/{2}", endpoint, messageType, client);
        }
    }
}