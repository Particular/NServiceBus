namespace NServiceBus.Core.Analyzer.Handlers;

using System.Linq;
using Utility;

public static partial class Handlers
{
    public static class Emitter
    {
        public static void EmitHandlerRegistryVariables(SourceWriter sourceWriter, string configurationVariable) =>
            sourceWriter.WriteLine($"""
                                    var settings = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings({configurationVariable});
                                    var messageHandlerRegistry = settings.GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                    var messageMetadataRegistry = settings.GetOrCreate<NServiceBus.Unicast.Messages.MessageMetadataRegistry>();
                                    """);

        public static void EmitHandlerRegistryCode(SourceWriter sourceWriter, HandlerSpec handlerSpec)
        {
            foreach (var registration in handlerSpec.Registrations)
            {
                var addType = registration.RegistrationType switch
                {
                    RegistrationType.MessageHandler or RegistrationType.StartMessageHandler => "Message",
                    RegistrationType.TimeoutHandler => "Timeout",
                    _ => "Message"
                };

                sourceWriter.WriteLine($"messageHandlerRegistry.Add{addType}HandlerForMessage<{registration.HandlerType}, {registration.MessageType}>();");
                var hierarchyLiteral = $"[{string.Join(", ", registration.MessageHierarchy.Select(type => $"typeof({type})"))}]";
                sourceWriter.WriteLine($"messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof({registration.MessageType}), {hierarchyLiteral});");
            }
        }
    }
}