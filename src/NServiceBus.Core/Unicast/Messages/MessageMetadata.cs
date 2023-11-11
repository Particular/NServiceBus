namespace NServiceBus.Unicast.Messages;

using System;
using System.Collections.Generic;

/// <summary>
/// Message metadata class.
/// </summary>
public class MessageMetadata
{
    /// <summary>
    /// Create a new instance of <see cref="MessageMetadata"/>.
    /// </summary>
    /// <param name="messageType">The type of the message this metadata belongs to.</param>
    public MessageMetadata(Type messageType) : this(messageType, null)
    {
    }

    /// <summary>
    /// Create a new instance of <see cref="MessageMetadata"/>.
    /// </summary>
    /// <param name="messageType">The type of the message this metadata belongs to.</param>
    /// <param name="messageHierarchy">the hierarchy of all message types implemented by the message this metadata belongs to.</param>
    public MessageMetadata(Type messageType, Type[] messageHierarchy)
    {
        MessageType = messageType;
        MessageHierarchy = messageHierarchy ?? Array.Empty<Type>();
        MessageHierarchySerialized = SerializeMessageHierarchy(MessageHierarchy);
    }

    /// <summary>
    /// The <see cref="Type" /> of the message instance.
    /// </summary>
    public Type MessageType { get; }


    /// <summary>
    /// The message instance hierarchy.
    /// </summary>
    public Type[] MessageHierarchy { get; }

    /// <summary>
    /// The message instance hierarchy serialized into a semicolon seperated string.
    /// </summary>
    public string MessageHierarchySerialized { get; }

    static string SerializeMessageHierarchy(Type[] messageHierarchy)
    {
        ICollection<string> assemblyQualifiedNames =
            messageHierarchy.Length == 0 ? Array.Empty<string>() : new List<string>(messageHierarchy.Length);
        foreach (var type in messageHierarchy)
        {
            var typeAssemblyQualifiedName = type.AssemblyQualifiedName;
            if (assemblyQualifiedNames.Contains(typeAssemblyQualifiedName))
            {
                continue;
            }

            assemblyQualifiedNames.Add(typeAssemblyQualifiedName);
        }

        return string.Join(";", assemblyQualifiedNames);
    }
}