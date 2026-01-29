namespace NServiceBus;

using System.Diagnostics.CodeAnalysis;

static class DynamicMemberTypeAccess
{
    public const DynamicallyAccessedMemberTypes Handler =
        DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.Interfaces
        | DynamicallyAccessedMemberTypes.PublicConstructors;

    public const DynamicallyAccessedMemberTypes Saga = Handler
                                                       | DynamicallyAccessedMemberTypes.NonPublicConstructors // Needed since we use RuntimeHelpers.GetUninitializedObject
                                                       | DynamicallyAccessedMemberTypes.NonPublicMethods; // we need this since we are calling the protected ConfigureHowToFindSaga

    public const DynamicallyAccessedMemberTypes SagaData = DynamicallyAccessedMemberTypes.PublicConstructors
                                                           | DynamicallyAccessedMemberTypes.PublicProperties;

    public const DynamicallyAccessedMemberTypes SagaFinder = Handler;

    public const DynamicallyAccessedMemberTypes SagaNotFoundHandler = Handler;

    public const DynamicallyAccessedMemberTypes Installer = Handler;

    public const DynamicallyAccessedMemberTypes Feature = Handler
                                                          | DynamicallyAccessedMemberTypes.NonPublicConstructors;
    public const DynamicallyAccessedMemberTypes FeatureStartupTask = Handler;
}