using NServiceBus;
using NServiceBus.Transport;

namespace InMemoryInlineWebApiBridge;

public static class NServiceBusConfiguration
{
    public const string MainEndpointName = "Samples.InMemoryInlineWebApiBridge.Main";
    const string ReactiveEndpointName = "Samples.InMemoryInlineWebApiBridge.Reactive";
    const string AzureEndpointName = "Samples.InMemoryInlineWebApiBridge.AzureReceiver";
    const string AzureErrorEndpointName = "error";

    public static IServiceCollection AddSampleEndpoints(
        this IServiceCollection services,
        IConfiguration configuration,
        InMemoryBroker broker,
        InMemoryStorage storage,
        DemoState state)
    {
        services.AddKeyedSingleton<DemoState>(MainEndpointName, state);
        services.AddKeyedSingleton<DemoState>(ReactiveEndpointName, state);
        services.AddNServiceBusEndpoint(CreateMainEndpoint(broker, storage, state, configuration), MainEndpointName);
        services.AddNServiceBusEndpoint(CreateReactiveEndpoint(broker, storage), ReactiveEndpointName);

        var connectionString = ResolveAzureServiceBusConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddKeyedSingleton<DemoState>(AzureEndpointName, state);
            services.AddNServiceBusEndpoint(CreateAzureEndpoint(connectionString, state), AzureEndpointName);
        }

        return services;
    }

    public static WebApplicationBuilder AddSampleBridge(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        InMemoryBroker broker,
        DemoState state)
    {
        var connectionString = ResolveAzureServiceBusConnectionString(configuration);

        var bridgeEnabled = !string.IsNullOrWhiteSpace(connectionString);
        state.SetBridgeEnabled(bridgeEnabled);

        if (!bridgeEnabled)
        {
            return builder;
        }

        var bridgeConfiguration = new BridgeConfiguration();

        var inMemoryTransport = new BridgeTransport(new InMemoryTransport(new InMemoryTransportOptions(broker)))
        {
            Name = "inmemory",
            AutoCreateQueues = true
        };
        inMemoryTransport.HasEndpoint(MainEndpointName);

        var azureTransport = new BridgeTransport(new AzureServiceBusTransport(connectionString!, TopicTopology.Default))
        {
            Name = "azureservicebus",
            AutoCreateQueues = true
        };
        azureTransport.HasEndpoint(AzureEndpointName);
        azureTransport.HasEndpoint(AzureErrorEndpointName);

        bridgeConfiguration.AddTransport(inMemoryTransport);
        bridgeConfiguration.AddTransport(azureTransport);

        builder.UseNServiceBusBridge(bridgeConfiguration);
        return builder;
    }

    static EndpointConfiguration CreateMainEndpoint(InMemoryBroker broker, InMemoryStorage storage, DemoState state, IConfiguration configuration)
    {
        var transport = new InMemoryTransport(new InMemoryTransportOptions(broker)
        {
            InlineExecution = new()
            {
                MoveToErrorQueueOnFailure = true
            }
        });

        var endpointConfiguration = CreateBaseEndpoint(MainEndpointName, transport, storage);
        endpointConfiguration.SendFailedMessagesTo(AzureErrorEndpointName);

        endpointConfiguration.AddHandler<RetryInlineCommandHandler>();
        endpointConfiguration.AddHandler<AlwaysFailInlineCommandHandler>();

        var routing = endpointConfiguration.UseTransport(transport);
        routing.RouteToEndpoint(typeof(NotifyReactiveEndpoint), ReactiveEndpointName);
        routing.RouteToEndpoint(typeof(BridgeToAzureCommand), AzureEndpointName);

        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Immediate(settings =>
        {
            settings.NumberOfRetries(2);
            settings.OnMessageBeingRetried((retry, _) =>
            {
                state.Record($"recoverability.immediate retryAttempt={retry.RetryAttempt} messageId={retry.MessageId}");
                return Task.CompletedTask;
            });
        });
        recoverability.Delayed(settings =>
        {
            settings.NumberOfRetries(0);
            settings.OnMessageBeingRetried((retry, _) =>
            {
                state.Record($"recoverability.delayed retryAttempt={retry.RetryAttempt} messageId={retry.MessageId}");
                return Task.CompletedTask;
            });
        });

        return endpointConfiguration;
    }

    static EndpointConfiguration CreateReactiveEndpoint(InMemoryBroker broker, InMemoryStorage storage)
    {
        var transport = new InMemoryTransport(new InMemoryTransportOptions(broker));
        var endpointConfiguration = CreateBaseEndpoint(ReactiveEndpointName, transport, storage);
        endpointConfiguration.UseTransport(transport);
        endpointConfiguration.AddHandler<NotifyReactiveEndpointHandler>();
        return endpointConfiguration;
    }

    static EndpointConfiguration CreateAzureEndpoint(string connectionString, DemoState state)
    {
        var transport = new AzureServiceBusTransport(connectionString, TopicTopology.Default);
        var endpointConfiguration = CreateBaseEndpoint(AzureEndpointName, transport, null);
        endpointConfiguration.UseTransport(transport);
        endpointConfiguration.AddHandler<BridgeToAzureCommandHandler>();
        return endpointConfiguration;
    }

    static EndpointConfiguration CreateBaseEndpoint(string endpointName, TransportDefinition transport, InMemoryStorage? storage)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.AssemblyScanner().Disable = true;

        if (storage is not null)
        {
            endpointConfiguration.UsePersistence<InMemoryPersistence>().Storage(storage);
        }

        endpointConfiguration.EnableInstallers();
        return endpointConfiguration;
    }

    static string? ResolveAzureServiceBusConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");
        }

        return connectionString;
    }
}
