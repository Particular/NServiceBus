namespace NServiceBus;

using System.Diagnostics.CodeAnalysis;

static class DynamicMemberTypeAccess
{
    public const DynamicallyAccessedMemberTypes Handler = Invokable;

    public const DynamicallyAccessedMemberTypes Saga = Handler
                                                       | DynamicallyAccessedMemberTypes.NonPublicConstructors // Needed since we use RuntimeHelpers.GetUninitializedObject when performing saga mapping
                                                       | DynamicallyAccessedMemberTypes.NonPublicMethods; // Needed since we are calling the protected ConfigureHowToFindSaga

    public const DynamicallyAccessedMemberTypes SagaData = DynamicallyAccessedMemberTypes.PublicConstructors
                                                           | DynamicallyAccessedMemberTypes.PublicProperties;

    public const DynamicallyAccessedMemberTypes SagaFinder = Invokable;

    public const DynamicallyAccessedMemberTypes SagaNotFoundHandler = Invokable;

    public const DynamicallyAccessedMemberTypes Installer = Invokable;

    public const DynamicallyAccessedMemberTypes Feature = Invokable
                                                          | DynamicallyAccessedMemberTypes.NonPublicConstructors; //TODO: Why was this needed?

    public const DynamicallyAccessedMemberTypes FeatureStartupTask = Invokable;

    const DynamicallyAccessedMemberTypes Invokable =
        DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.Interfaces
        | DynamicallyAccessedMemberTypes.PublicConstructors;
}