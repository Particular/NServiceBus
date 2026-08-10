namespace NServiceBus.Testing;

using System.Diagnostics.CodeAnalysis;

static class DynamicMemberTypeAccess
{
    public const DynamicallyAccessedMemberTypes Message = DynamicallyAccessedMemberTypes.PublicConstructors
                                                          | DynamicallyAccessedMemberTypes.NonPublicConstructors
                                                          | DynamicallyAccessedMemberTypes.PublicProperties
                                                          | DynamicallyAccessedMemberTypes.Interfaces;

    public const string RuntimeTypeRoutingTrimmingMessage = "When trimming is enabled, routing a message using its runtime type cannot be statically analyzed by the trimmer. Use the generic overload or, when the message type is not known at compile time, the overload accepting an explicit Type.";

}
