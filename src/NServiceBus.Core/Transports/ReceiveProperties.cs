namespace NServiceBus.Transport;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Properties received from the transport that can be propagated to outgoing dispatch operations.
/// Transports populate this with native message metadata that should survive audit and error operations.
/// </summary>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Name reflects domain semantics, not collection implementation.")]
public sealed class ReceiveProperties : IReadOnlyDictionary<string, string>
{
    readonly Dictionary<string, string> properties;

    /// <summary>
    /// An empty <see cref="ReceiveProperties" /> instance.
    /// </summary>
    public static ReceiveProperties Empty { get; } = new();

    /// <summary>
    /// Creates an empty instance of <see cref="ReceiveProperties" />.
    /// </summary>
    public ReceiveProperties() => properties = [];

    /// <summary>
    /// Creates a <see cref="ReceiveProperties" /> from the provided dictionary.
    /// The dictionary is stored by reference — do not mutate it after passing to this constructor.
    /// </summary>
    public ReceiveProperties(Dictionary<string, string> dictionary) => properties = dictionary;

    /// <inheritdoc />
    public string this[string key] => properties[key];

    /// <inheritdoc />
    public IEnumerable<string> Keys => properties.Keys;

    /// <inheritdoc />
    public IEnumerable<string> Values => properties.Values;

    /// <inheritdoc />
    public int Count => properties.Count;

    /// <inheritdoc />
    public bool ContainsKey(string key) => properties.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, out string value) => properties.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => properties.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}