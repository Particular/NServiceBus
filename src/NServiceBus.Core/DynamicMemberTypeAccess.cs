namespace NServiceBus;

using System.Diagnostics.CodeAnalysis;

static class DynamicMemberTypeAccess
{
    public const DynamicallyAccessedMemberTypes SagaData = DynamicallyAccessedMemberTypes.PublicConstructors
                                                           | DynamicallyAccessedMemberTypes.PublicProperties;

    public const DynamicallyAccessedMemberTypes Saga = Handler
                                                       | DynamicallyAccessedMemberTypes.NonPublicConstructors // Needed since we use RuntimeHelpers.GetUninitializedObject when performing saga mapping
                                                       | DynamicallyAccessedMemberTypes.NonPublicMethods; // Needed since we are calling the protected ConfigureHowToFindSaga

    public const DynamicallyAccessedMemberTypes Handler = InvokableWithDependencyInjection;

    public const DynamicallyAccessedMemberTypes SagaFinder = InvokableWithDependencyInjection;

    public const DynamicallyAccessedMemberTypes SagaNotFoundHandler = InvokableWithDependencyInjection;

    public const DynamicallyAccessedMemberTypes Installer = InvokableWithDependencyInjection;

    public const DynamicallyAccessedMemberTypes FeatureStartupTask = InvokableWithDependencyInjection;

    public const DynamicallyAccessedMemberTypes Feature = Invokable
                                                          | DynamicallyAccessedMemberTypes.NonPublicConstructors; //TODO: Why was this needed?

    const DynamicallyAccessedMemberTypes InvokableWithDependencyInjection = Invokable
                                                                            | DynamicallyAccessedMemberTypes.PublicConstructors;
    public const DynamicallyAccessedMemberTypes InitializationExtension = Invokable;

    const DynamicallyAccessedMemberTypes Invokable =
        DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.Interfaces
        | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
}