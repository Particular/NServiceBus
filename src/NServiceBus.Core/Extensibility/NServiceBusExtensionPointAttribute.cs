namespace NServiceBus.Extensibility;

using System;

/// <summary>
///
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class NServiceBusExtensionPointAttribute : Attribute
{
    /// <summary>
    ///
    /// </summary>
    public string RegistrationMethodName { get; set; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="registrationMethodName"></param>
    public NServiceBusExtensionPointAttribute(string registrationMethodName) => RegistrationMethodName = registrationMethodName;
}
