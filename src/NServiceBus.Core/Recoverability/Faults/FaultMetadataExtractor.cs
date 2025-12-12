#nullable enable

namespace NServiceBus;

using System;
using System.Collections;
using System.Collections.Generic;
using Faults;
using Transport;

class FaultMetadataExtractor(Dictionary<string, string> staticFaultMetadata, Action<Dictionary<string, string>> headerCustomizations)
{
    public Dictionary<string, string> Extract(ErrorContext errorContext)
    {
        var metadata = new Dictionary<string, string>(staticFaultMetadata)
        {
            [FaultsHeaderKeys.FailedQ] = errorContext.ReceiveAddress
        };

        SetExceptionMetadata(metadata, errorContext.Exception);

        headerCustomizations(metadata);

        return metadata;
    }

    static void SetExceptionMetadata(Dictionary<string, string> headers, Exception e)
    {
        headers[FaultsHeaderKeys.ExceptionType] = e.GetType().FullName!;

        if (e.InnerException != null)
        {
            headers[FaultsHeaderKeys.InnerExceptionType] = e.InnerException.GetType().FullName!;
        }

        if (e.HelpLink is not null)
        {
            headers[FaultsHeaderKeys.HelpLink] = e.HelpLink;
        }
        headers[FaultsHeaderKeys.Message] = Truncate(e.GetMessage(), 16384);
        if (e.Source is not null)
        {
            headers[FaultsHeaderKeys.Source] = e.Source;
        }
        headers[FaultsHeaderKeys.StackTrace] = e.ToString();
        headers[FaultsHeaderKeys.TimeOfFailure] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);

        foreach (DictionaryEntry entry in e.Data)
        {
            if (entry.Value is not { } value)
            {
                continue;
            }
            headers[$"{FaultsHeaderKeys.ExceptionInfoDataPrefix}{entry.Key}"] = value.ToString()!;
        }
    }

    static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value)
            ? value
            : value.Length <= maxLength
                ? value
                : value[..maxLength];
}