namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Messaging;

    /// <summary>
    /// The signature of the label generator used by <see cref="MsmqConfigurationExtensions.ApplyLabelToMessages"/>.
    /// </summary>
    /// <param name="headers">The message headers of the message at the point before it is placed on the wire.</param>
    /// <returns>
    /// A <see cref="string"/> used for the <see cref="Message.Label"/> or an empty string for no label. The returned value must be at most 240 characters.
    /// </returns>
    public delegate string MsmqLabelGenerator(IReadOnlyDictionary<string, string> headers);
}