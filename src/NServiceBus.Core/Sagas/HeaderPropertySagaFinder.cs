#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Sagas;

class HeaderPropertySagaFinder<TSagaData>(string headerName, string correlationPropertyName, Type correlationPropertyType, Type messageType) : ICoreSagaFinder
    where TSagaData : class, IContainSagaData
{
    public async Task<IContainSagaData> Find(IServiceProvider builder, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
    {
        if (!messageHeaders.TryGetValue(headerName, out var messageHeaderValue))
        {
            var saga = context.Get<ActiveSagaInstance>();
            var sagaEntityName = saga.Metadata.Name;
            var messageName = messageType.FullName;

            throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} is missing a header used for correlation: {headerName}.");
        }

        object convertedHeaderValue;

        try
        {
            convertedHeaderValue = ConvertCorrelationHeaderValue(correlationPropertyType, messageHeaderValue);
        }
        catch (Exception exception)
        {
            var saga = context.Get<ActiveSagaInstance>();
            var sagaEntityName = saga.Metadata.Name;
            var messageName = messageType.FullName;

            throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} contains correlation header {headerName} value that cannot be cast to correlation property type {correlationPropertyType}: {messageHeaderValue}", exception);
        }

        var lookupValues = context.GetOrCreate<SagaLookupValues>();
        lookupValues.Add<TSagaData>(correlationPropertyName, convertedHeaderValue);

        var persister = builder.GetRequiredService<ISagaPersister>();
        if (correlationPropertyName.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            return await persister.Get<TSagaData>((Guid)convertedHeaderValue, storageSession, context, cancellationToken).ConfigureAwait(false);
        }

        return await persister.Get<TSagaData>(correlationPropertyName, convertedHeaderValue, storageSession, context, cancellationToken).ConfigureAwait(false);
    }

    static object ConvertCorrelationHeaderValue(Type correlationPropertyType, string? headerValue)
    {
        if (correlationPropertyType == typeof(string))
        {
            return headerValue ?? string.Empty; // backward compatibility: treat null header as empty string for string correlation properties
        }

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            ThrowNotSupportedExceptionForNullOrEmptyHeaderValue(correlationPropertyType);
        }

        return correlationPropertyType switch
        {
            _ when correlationPropertyType == typeof(Guid) =>
                Guid.Parse(headerValue, CultureInfo.InvariantCulture),
            _ when correlationPropertyType == typeof(long) =>
                long.Parse(headerValue, CultureInfo.InvariantCulture),
            _ when correlationPropertyType == typeof(ulong) =>
                ulong.Parse(headerValue, CultureInfo.InvariantCulture),
            _ when correlationPropertyType == typeof(int) =>
                int.Parse(headerValue, CultureInfo.InvariantCulture),
            _ when correlationPropertyType == typeof(uint) =>
                uint.Parse(headerValue, CultureInfo.InvariantCulture),
            _ when correlationPropertyType == typeof(short) =>
                int.Parse(headerValue, CultureInfo.InvariantCulture),
            _ when correlationPropertyType == typeof(ushort) =>
                uint.Parse(headerValue, CultureInfo.InvariantCulture),
            _ => ThrowUnsupportedExceptionForCorrelationPropertyType(correlationPropertyType)
        };
    }

    [DoesNotReturn]
    static void ThrowNotSupportedExceptionForNullOrEmptyHeaderValue(Type correlationPropertyType)
        => throw new NotSupportedException($"Cannot convert null or empty header value to non-string correlation type {correlationPropertyType}.");

    [DoesNotReturn]
    static object ThrowUnsupportedExceptionForCorrelationPropertyType(Type correlationPropertyType)
        => throw new NotSupportedException($"Unsupported correlation property type: {correlationPropertyType}");
}