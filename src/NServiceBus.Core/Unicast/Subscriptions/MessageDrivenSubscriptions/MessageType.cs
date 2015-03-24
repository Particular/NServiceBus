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
        public MessageType(Type type)
        {
            Guard.AgainstNull(type, "type");
            Version = type.Assembly.GetName().Version;
            TypeName = type.FullName;
        }

        /// <summary>
        /// Initializes the message type from the given string. 
        /// </summary>
        public MessageType(string messageTypeString)
        {
            Guard.AgainstNullAndEmpty(messageTypeString, "messageTypeString");
            var parts = messageTypeString.Split(',');
            Version = ParseVersion(messageTypeString); 
            TypeName = parts.First();
        }

        /// <summary>
        /// Initializes the message type from the given string. 
        /// </summary>
        public MessageType(string typeName, string versionString)
        {
            Guard.AgainstNullAndEmpty(typeName, "typeName");
            Guard.AgainstNullAndEmpty(versionString, "versionString");
            Version = ParseVersion(versionString);
            TypeName = typeName;
        }

        /// <summary>
        /// Initializes the message type from the given string. 
        /// </summary>
        public MessageType(string typeName,Version version)
        {
            Guard.AgainstNullAndEmpty(typeName, "typeName");
            Guard.AgainstNull(version, "version");
            Version = version;
            TypeName = typeName;
        }
    
        Version ParseVersion(string versionString)
        {
            const string version = "Version=";
            var index = versionString.IndexOf(version);
           
            if(index >= 0)
                versionString = versionString.Substring(index + version.Length)
                    .Split(',').First();
            return Version.Parse(versionString);
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
        /// Overridden to append Version along with Type Name
        /// </summary>
        public override string ToString()
        {
            return TypeName + ", Version=" + Version;
        }

        /// <summary>
        /// Equality, only major version is used
        /// </summary>
        public bool Equals(MessageType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.TypeName, TypeName) && other.Version.Major == Version.Major;
        }

        /// <summary>
        /// Equality, only Type is same
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (MessageType)) return false;
            return Equals((MessageType) obj);
        }

        /// <summary>
        /// Gets Hash Code
        /// </summary>
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
        public static bool operator ==(MessageType left, MessageType right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Equality
        /// </summary>
        public static bool operator !=(MessageType left, MessageType right)
        {
            return !Equals(left, right);
        }
    }
}