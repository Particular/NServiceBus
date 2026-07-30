namespace NServiceBus.Testing;

using System.Diagnostics.CodeAnalysis;

static class DynamicMemberTypeAccess
{
    public const DynamicallyAccessedMemberTypes Message = DynamicallyAccessedMemberTypes.PublicConstructors
                                                          | DynamicallyAccessedMemberTypes.NonPublicConstructors
                                                          | DynamicallyAccessedMemberTypes.PublicProperties
                                                          | DynamicallyAccessedMemberTypes.Interfaces;
}
