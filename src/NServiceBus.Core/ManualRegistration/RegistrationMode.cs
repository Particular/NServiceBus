namespace NServiceBus;

/// <summary>
/// Specifies how types (handlers, messages, sagas, etc.) are discovered and registered in an endpoint.
/// </summary>
public enum RegistrationMode
{
    /// <summary>
    /// Default behavior. NServiceBus automatically scans assemblies at runtime to discover handlers,
    /// messages, sagas, and other extension points using reflection.
    /// </summary>
    AssemblyScanning,

    /// <summary>
    /// Explicit, declarative registration. Assembly scanning is disabled and all types must be
    /// registered manually using the Register* methods (e.g., RegisterHandler, RegisterMessage, etc.).
    /// This mode is required for AOT compilation and enables multiple independent endpoints in a single process.
    /// </summary>
    Manual,

    /// <summary>
    /// Build-time source generator produces registration code that calls the same manual registration APIs.
    /// Assembly scanning is disabled and types are registered via generated code at compile time.
    /// Provides the benefits of Manual mode with automatic discovery at build time.
    /// </summary>
    SourceGenerated
}

