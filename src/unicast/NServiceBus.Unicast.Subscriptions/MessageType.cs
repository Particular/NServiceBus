namespace NServiceBus.Unicast.Subscriptions
{
    using System;
    using System.Linq;

    /// <summary>
    /// Representation of a message type that clients can be subscribed to
    /// </summary>
    public class MessageType
    {
        /// <summary>
        /// Initializes the message type from the given type
        /// </summary>
        /// <param name="type"></param>
        public MessageType(Type type)
        {
            Version = type.Assembly.GetName().Version;
            TypeName = type.FullName;
        }

        /// <summary>
        /// Initializes the message type from the given string. 
        /// </summary>
        /// <param name="messageTypeString"></param>
        public MessageType(string messageTypeString)
        {
            var parts = messageTypeString.Split(',');

            var versionString = parts
                .Single(p=>p.Contains("Version"))
                .Substring("Version=".Length +1);

            Version = new Version(versionString);
            TypeName = parts.First();
        }
    

        /// <summary>
        /// TypeName of the message
        /// </summary>
        public string TypeName { get;private  set; }

        /// <summary>
        /// Version of the message
        /// </summary>
        public Version Version { get; private set; }


        /// <summary>
        /// Equality, only major version is used
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(MessageType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.TypeName, TypeName) && other.Version.Major == Version.Major;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (MessageType)) return false;
            return Equals((MessageType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TypeName.GetHashCode()*397) ^ Version.GetHashCode();
            }
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(MessageType left, MessageType right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(MessageType left, MessageType right)
        {
            return !Equals(left, right);
        }
    }
}