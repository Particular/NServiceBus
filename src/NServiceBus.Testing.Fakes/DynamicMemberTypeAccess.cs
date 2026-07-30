namespace NServiceBus.Testing;

using System.Diagnostics.CodeAnalysis;

static class DynamicMemberTypeAccess
{
    public const DynamicallyAccessedMemberTypes Message = DynamicallyAccessedMemberTypes.PublicConstructors
                                                          | DynamicallyAccessedMemberTypes.NonPublicConstructors
                                                          | DynamicallyAccessedMemberTypes.PublicProperties
                                                          | DynamicallyAccessedMemberTypes.Interfaces;

    public const string RuntimeTypeRoutingTrimmingMessage = "Routing a message using its runtime type cannot be statically analyzed by the trimmer. Use the generic overload and specify the message type.";
}
