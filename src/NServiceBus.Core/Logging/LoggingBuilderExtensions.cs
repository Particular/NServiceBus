#nullable enable

namespace NServiceBus;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddNServiceBusLoggingProviders(this ILoggingBuilder builder, string loggingDirectory, LogLevel logLevel)
    {
        builder.AddNServiceBusRollingFileProvider(options =>
        {
            options.Directory = loggingDirectory;
            options.LogLevel = logLevel;
        });
        builder.AddNServiceBusColoredConsoleProvider();

        if (logLevel != LogLevel.Information)
        {
            builder.SetMinimumLevel(logLevel);
        }

        return builder;
    }

    static ILoggingBuilder AddNServiceBusRollingFileProvider(this ILoggingBuilder builder, Action<RollingLoggerProviderOptions> configure)
    {
        builder.AddConfiguration();

        builder.Services.Configure(configure);

        builder.Services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(sp =>
        {
            var rollingOptions = sp.GetRequiredService<IOptions<RollingLoggerProviderOptions>>();
            return new ConfigureOptions<LoggerFilterOptions>(filterOptions =>
            {
                filterOptions.Rules.Add(new LoggerFilterRule(
                    providerName: typeof(RollingLoggerProvider).FullName,
                    categoryName: null,
                    logLevel: rollingOptions.Value.LogLevel,
                    filter: null));
            });
        });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, RollingLoggerProvider>());
        return builder;
    }

    static ILoggingBuilder AddNServiceBusColoredConsoleProvider(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ColoredConsoleLoggerProvider>());
        return builder;
    }
}