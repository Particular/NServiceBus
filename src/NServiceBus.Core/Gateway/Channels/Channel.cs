namespace NServiceBus.Gateway.Channels
{
    using System;

    public class ReceiveChannel : Channel
    {
        public int NumberOfWorkerThreads { get; set; }
        public bool Default { get; set; }

        public override string ToString()
        {
            return base.ToString() + "NumberOfWorkerThreads=" + NumberOfWorkerThreads + "Default=" + Default;
        }
    }

    public class Channel : IEquatable<Channel>
    {
        public string Type { get; set; }
        public string Address { get; set; }

        public bool Equals(Channel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.Type, Type) && Equals(other.Address, Address);
        }

        public static Channel Parse(string s)
        {
            var parts = s.Split(',');

            return new Channel
            {
                Type = parts[0],
                Address = parts[1]
            };
        }

        public override string ToString()
        {
            return string.Format("{0},{1}", Type, Address);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof(Channel))
            {
                return false;
            }
            return Equals((Channel) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type != null ? Type.GetHashCode() : 0)*397) ^ (Address != null ? Address.GetHashCode() : 0);
            }
        }

        public static bool operator ==(Channel left, Channel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Channel left, Channel right)
        {
            return !Equals(left, right);
        }
    }
}