namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MessageInterfaces;
using Serialization;
using Settings;
using Unicast.Messages;

/// <summary>
/// Defines the capabilities of the XML serializer.
/// </summary>
[RequiresUnreferencedCode(TrimmingMessage)]
public class XmlSerializer : SerializationDefinition
{
    internal const string TrimmingMessage = "XmlSerializer is not supported in trimming scenarios.";
    /// <summary>
    /// Provides a factory method for building a message serializer.
    /// </summary>
    public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return mapper =>
        {
            var conventions = settings.Get<Conventions>();
            var serializer = new XmlMessageSerializer(mapper, conventions);

            if (settings.TryGet(CustomNamespaceConfigurationKey, out string customNamespace))
            {
                serializer.Namespace = customNamespace;
            }

            if (settings.TryGet(SkipWrappingRawXml, out bool skipWrappingRawXml))
            {
                serializer.SkipWrappingRawXml = skipWrappingRawXml;
            }

            if (settings.TryGet(SanitizeInput, out bool sanitizeInput))
            {
                serializer.SanitizeInput = sanitizeInput;
            }

            var registry = settings.Get<MessageMetadataRegistry>();
            var messageTypes = registry.GetAllMessages().Select(m => m.MessageType);

            serializer.Initialize(messageTypes);
            return serializer;
        };
    }

    internal const string CustomNamespaceConfigurationKey = "XmlSerializer.CustomNamespace";
    internal const string SkipWrappingRawXml = "XmlSerializer.SkipWrappingRawXml";
    internal const string SanitizeInput = "XmlSerializer.SanitizeInput";
}