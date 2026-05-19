#nullable enable

namespace NServiceBus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddNServiceBusLoggingProviders(this ILoggingBuilder builder, string loggingDirectory, LogLevel logLevel)
    {
        builder.Services.Configure<RollingLoggerProviderOptions>(o =>
        {
            o.Directory = loggingDirectory;
            o.LogLevel = logLevel;
        });
        builder.Services.AddSingleton<ILoggerProvider, RollingLoggerProvider>();
        builder.Services.AddSingleton<ILoggerProvider, ColoredConsoleLoggerProvider>();

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

        if (logLevel != LogLevel.Information)
        {
            builder.SetMinimumLevel(logLevel);
        }

        return builder;
    }
}
