#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;

public abstract record EnvelopeUnwrapResult
{
    EnvelopeUnwrapResult() { }

    /// <summary>
    /// The given envelop is not supported by the envelope handler
    /// </summary>
    public sealed record UnsupportedEnvelope() : EnvelopeUnwrapResult;

    /// <summary>
    /// The envelope handler successfully unwrapped the message envelope
    /// </summary>
    /// <param name="Headers">The unwrapped message headers</param>
    /// <param name="Body">The unwrapped message body</param>
    public sealed record Success(Dictionary<string, string> Headers, ReadOnlyMemory<byte> Body) : EnvelopeUnwrapResult;

    /// <summary>
    /// The envelope handler failed in either detecting the envelope type or in unwrapping the envelope message
    /// </summary>
    /// <param name="Exception">The handling failure</param>
    public sealed record Malformed(Exception? Exception) : EnvelopeUnwrapResult;
}