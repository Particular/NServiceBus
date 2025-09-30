namespace NServiceBus;

using System;

/// <summary>
/// Extensions for manual type registration.
/// </summary>
public static class ManualRegistrationConfigExtensions
{
    /// <summary>
    /// Manually registers a message handler type that implements <see cref="IHandleMessages{T}"/>.
    /// This allows explicit handler registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="THandler">The handler type to register.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterHandler<THandler>(this EndpointConfiguration config)
        where THandler : class
    {
        RegisterHandler(config, typeof(THandler));
    }

    /// <summary>
    /// Manually registers a message handler type that implements <see cref="IHandleMessages{T}"/>.
    /// This allows explicit handler registration as an alternative to assembly scanning.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="handlerType">The handler type to register.</param>
    public static void RegisterHandler(this EndpointConfiguration config, Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        if (!config.Settings.TryGet(out ManuallyRegisteredHandlers handlers))
        {
            handlers = new ManuallyRegisteredHandlers();
            config.Settings.Set(handlers);
        }

        handlers.HandlerTypes.Add(handlerType);
    }

    /// <summary>
    /// Manually registers a saga type with its associated saga data type.
    /// This allows explicit saga registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to register.</typeparam>
    /// <typeparam name="TSagaData">The saga data type.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterSaga<TSaga, TSagaData>(this EndpointConfiguration config)
        where TSaga : Saga<TSagaData>
        where TSagaData : class, IContainSagaData, new()
    {
        RegisterSaga(config, typeof(TSaga), typeof(TSagaData));
    }

    /// <summary>
    /// Manually registers a saga type with its associated saga data type.
    /// This allows explicit saga registration as an alternative to assembly scanning.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="sagaType">The saga type to register.</param>
    /// <param name="sagaDataType">The saga data type.</param>
    public static void RegisterSaga(this EndpointConfiguration config, Type sagaType, Type sagaDataType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        ArgumentNullException.ThrowIfNull(sagaDataType);

        if (!config.Settings.TryGet(out ManuallyRegisteredSagas sagas))
        {
            sagas = new ManuallyRegisteredSagas();
            config.Settings.Set(sagas);
        }

        sagas.Sagas.Add(new SagaRegistration(sagaType, sagaDataType));
    }

    /// <summary>
    /// Manually registers a message type.
    /// This allows explicit message registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="TMessage">The message type to register.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterMessage<TMessage>(this EndpointConfiguration config)
        where TMessage : class
    {
        RegisterMessage(config, typeof(TMessage));
    }

    /// <summary>
    /// Manually registers a message type.
    /// This allows explicit message registration as an alternative to assembly scanning.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="messageType">The message type to register.</param>
    public static void RegisterMessage(this EndpointConfiguration config, Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (!config.Settings.TryGet(out ManuallyRegisteredMessages messages))
        {
            messages = new ManuallyRegisteredMessages();
            config.Settings.Set(messages);
        }

        messages.MessageTypes.Add(messageType);
    }

    /// <summary>
    /// Manually registers an event type.
    /// This allows explicit event registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="TEvent">The event type to register.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterEvent<TEvent>(this EndpointConfiguration config)
        where TEvent : class, IEvent
    {
        RegisterMessage(config, typeof(TEvent));
    }

    /// <summary>
    /// Manually registers a command type.
    /// This allows explicit command registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="TCommand">The command type to register.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterCommand<TCommand>(this EndpointConfiguration config)
        where TCommand : class, ICommand
    {
        RegisterMessage(config, typeof(TCommand));
    }

    /// <summary>
    /// Manually registers an installer type that implements <see cref="Installation.INeedToInstallSomething"/>.
    /// This allows explicit installer registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="TInstaller">The installer type to register.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterInstaller<TInstaller>(this EndpointConfiguration config)
        where TInstaller : class, Installation.INeedToInstallSomething
    {
        RegisterInstaller(config, typeof(TInstaller));
    }

    /// <summary>
    /// Manually registers an installer type that implements <see cref="Installation.INeedToInstallSomething"/>.
    /// This allows explicit installer registration as an alternative to assembly scanning.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="installerType">The installer type to register.</param>
    public static void RegisterInstaller(this EndpointConfiguration config, Type installerType)
    {
        ArgumentNullException.ThrowIfNull(installerType);

        if (!config.Settings.TryGet(out ManuallyRegisteredInstallers installers))
        {
            installers = new ManuallyRegisteredInstallers();
            config.Settings.Set(installers);
        }

        installers.InstallerTypes.Add(installerType);
    }

    /// <summary>
    /// Manually registers a feature type that inherits from <see cref="Features.Feature"/>.
    /// This allows explicit feature registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="TFeature">The feature type to register.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterFeature<TFeature>(this EndpointConfiguration config)
        where TFeature : Features.Feature
    {
        RegisterFeature(config, typeof(TFeature));
    }

    /// <summary>
    /// Manually registers a feature type that inherits from <see cref="Features.Feature"/>.
    /// This allows explicit feature registration as an alternative to assembly scanning.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="featureType">The feature type to register.</param>
    public static void RegisterFeature(this EndpointConfiguration config, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(featureType);

        if (!config.Settings.TryGet(out ManuallyRegisteredFeatures features))
        {
            features = new ManuallyRegisteredFeatures();
            config.Settings.Set(features);
        }

        features.FeatureTypes.Add(featureType);
    }

    /// <summary>
    /// Manually registers an initializer type that implements <see cref="INeedInitialization"/>.
    /// This allows explicit initializer registration as an alternative to assembly scanning.
    /// </summary>
    /// <typeparam name="TInitializer">The initializer type to register.</typeparam>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    public static void RegisterInitializer<TInitializer>(this EndpointConfiguration config)
        where TInitializer : class, INeedInitialization
    {
        RegisterInitializer(config, typeof(TInitializer));
    }

    /// <summary>
    /// Manually registers an initializer type that implements <see cref="INeedInitialization"/>.
    /// This allows explicit initializer registration as an alternative to assembly scanning.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="initializerType">The initializer type to register.</param>
    public static void RegisterInitializer(this EndpointConfiguration config, Type initializerType)
    {
        ArgumentNullException.ThrowIfNull(initializerType);

        if (!config.Settings.TryGet(out ManuallyRegisteredInitializers initializers))
        {
            initializers = new ManuallyRegisteredInitializers();
            config.Settings.Set(initializers);
        }

        initializers.InitializerTypes.Add(initializerType);
    }
}
