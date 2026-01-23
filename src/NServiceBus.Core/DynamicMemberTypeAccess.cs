namespace NServiceBus;

using System.Diagnostics.CodeAnalysis;

static class DynamicMemberTypeAccess
{
    public const DynamicallyAccessedMemberTypes Handler =
        DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.Interfaces
        | DynamicallyAccessedMemberTypes.AllConstructors;

    public const DynamicallyAccessedMemberTypes Saga = Handler
                                                       | DynamicallyAccessedMemberTypes.PublicProperties;

    public const DynamicallyAccessedMemberTypes SagaData = DynamicallyAccessedMemberTypes.AllConstructors
                                                           | DynamicallyAccessedMemberTypes.PublicProperties;

    public const DynamicallyAccessedMemberTypes SagaFinder = DynamicallyAccessedMemberTypes.AllConstructors
                                                             | DynamicallyAccessedMemberTypes.Interfaces
                                                             | DynamicallyAccessedMemberTypes.PublicMethods;

    public const DynamicallyAccessedMemberTypes SagaNotFoundHandler = DynamicallyAccessedMemberTypes.AllConstructors
                                                                      | DynamicallyAccessedMemberTypes.Interfaces
                                                                      | DynamicallyAccessedMemberTypes.PublicMethods;
}