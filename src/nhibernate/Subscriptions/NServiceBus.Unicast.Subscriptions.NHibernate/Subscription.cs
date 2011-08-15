namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    /// <summary>
    /// Enity containing subscription data
    /// </summary>
    public class Subscription
    {
        public virtual string SubscriberEndpoint { get; set; }
        public virtual string MessageType { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Subscription)) return false;
            return Equals((Subscription) obj);
        }

        public virtual bool Equals(Subscription other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.SubscriberEndpoint, SubscriberEndpoint) && Equals(other.MessageType, MessageType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SubscriberEndpoint != null ? SubscriberEndpoint.GetHashCode() : 0)*397) ^ (MessageType != null ? MessageType.GetHashCode() : 0);
            }
        }
    }
}