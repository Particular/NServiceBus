namespace NServiceBus.Unicast.Subscriptions
{
    using Microsoft.WindowsAzure.Storage.Table.DataServices;

    /// <summary>
    /// Enity containing subscription data
    /// </summary>
    public class Subscription : TableServiceEntity
    {
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Subscription)) return false;
            return Equals((Subscription)obj);
        }

        public virtual bool Equals(Subscription other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.RowKey, RowKey) && Equals(other.PartitionKey, PartitionKey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((RowKey != null ? RowKey.GetHashCode() : 0) * 397) ^ (PartitionKey != null ? PartitionKey.GetHashCode() : 0);
            }
        }
    }
}