namespace NServiceBus
{
    using System;

    /// <summary>
    /// Attribute to indicate that a message is recoverable - this is now the default.
    /// </summary>
    /// <remarks>
    /// This attribute should be applied to classes that implement <see cref="IMessage"/>
    /// to indicate that they should be treated as a recoverable message.  A recoverable 
    /// message is stored locally at every step along the route so that in the event of
    /// a failure of a machine along the route a copy of the message will be recovered and
    /// delivery will continue when the machine is brought back online.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    [ObsoleteEx(TreatAsErrorFromVersion = "4.0")]
    public class RecoverableAttribute : Attribute
    {
    }
}