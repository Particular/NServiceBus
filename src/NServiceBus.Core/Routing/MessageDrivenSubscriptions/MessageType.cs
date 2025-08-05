namespace NServiceBus.Unicast.Subscriptions;

using System;

/// <summary>
/// Representation of a message type that clients can be subscribed to.
/// </summary>
public class MessageType
{
    /// <summary>
    /// Initializes the message type from the given type.
    /// </summary>
    public MessageType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Version = type.Assembly.GetName().Version;
        TypeName = type.FullName;
    }

    /// <summary>
    /// Initializes the message type from the given string.
    /// </summary>
    public MessageType(string messageTypeString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageTypeString);

        Version = ParseVersion(messageTypeString);

        var messageTypeSpan = messageTypeString.AsSpan();
        var index = messageTypeSpan.IndexOf(',');

        if (index >= 0)
        {
            messageTypeSpan = messageTypeSpan[..index];
        }

        TypeName = messageTypeSpan.ToString();
    }

    /// <summary>
    /// Initializes the message type from the given string.
    /// </summary>
    public MessageType(string typeName, string versionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(versionString);
        Version = ParseVersion(versionString);
        TypeName = typeName;
    }

    /// <summary>
    /// Initializes the message type from the given string.
    /// </summary>
    public MessageType(string typeName, Version version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentNullException.ThrowIfNull(version);
        Version = version;
        TypeName = typeName;
    }


    /// <summary>
    /// TypeName of the message.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Version of the message.
    /// </summary>
    public Version Version { get; }

    static Version ParseVersion(ReadOnlySpan<char> input)
    {
        const string versionPrefix = "Version=";
        var versionPrefixIndex = input.IndexOf(versionPrefix);

        if (versionPrefixIndex >= 0)
        {
            input = input[(versionPrefixIndex + versionPrefix.Length)..];

            var firstComma = input.IndexOf(',');
            if (firstComma > 0)
            {
                input = input[..firstComma];
            }
        }

        return Version.Parse(input);
    }

    /// <summary>
    /// Overridden to append Version along with Type Name.
    /// </summary>
    public override string ToString()
    {
        return TypeName + ", Version=" + Version;
    }


    /// <summary>
    /// Equality only compares the <see cref="TypeName"/> and ignores versions.
    /// </summary>
    public bool Equals(MessageType other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return Equals(other.TypeName, TypeName);
    }

    /// <summary>
    /// Equality only compares the <see cref="TypeName"/> and ignores versions.
    /// </summary>
    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != typeof(MessageType))
        {
            return false;
        }
        return Equals((MessageType)obj);
    }

    /// <summary>
    /// Gets Hash Code.
    /// </summary>
    public override int GetHashCode()
    {
        return TypeName.GetHashCode();
    }

    /// <summary>
    /// Equality.
    /// </summary>
    public static bool operator ==(MessageType left, MessageType right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Equality.
    /// </summary>
    public static bool operator !=(MessageType left, MessageType right)
    {
        return !Equals(left, right);
    }
}