#nullable enable

namespace NServiceBus.Unicast.Messages;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

public partial class MessageMetadataRegistry
{
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Dynamic type loading is best-effort; when trimming removes a type, resolution falls back to known registered message metadata.")]
    Type? GetType(string messageTypeIdentifier)
    {
        if (allowDynamicTypeLoading)
        {
            try
            {
                return Type.GetType(messageTypeIdentifier);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Message type identifier '{messageTypeIdentifier}' could not be loaded", ex);
            }
        }
        else
        {
            Logger.Warn($"Unknown message type identifier '{messageTypeIdentifier}'. Dynamic type loading is disabled. Make sure the type is loaded before starting the endpoint or enable dynamic type loading.");
        }

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Runtime hierarchy inference is used for message types not pre-registered with source-generated hierarchy metadata, including scanned, dynamically loaded, published-only, and legacy message types.")]
    Type[] GetRuntimeMessageHierarchy(Type messageType)
    {
        var parentTypes = new List<Type>(messageType.GetInterfaces());

        var currentBaseType = messageType.BaseType;
        var objectType = typeof(object);
        while (currentBaseType != null && currentBaseType != objectType)
        {
            parentTypes.Add(currentBaseType);
            currentBaseType = currentBaseType.BaseType;
        }

        return [.. parentTypes
            .Where(isMessageType)
            .OrderByDescending(static type =>
            {
                if (type.IsInterface)
                {
                    return type.GetInterfaces().Length;
                }

                var result = 0;
                while (type.BaseType != null)
                {
                    result++;
                    type = type.BaseType;
                }

                return result;
            })];
    }
}
