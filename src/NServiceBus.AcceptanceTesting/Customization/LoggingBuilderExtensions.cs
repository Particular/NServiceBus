namespace NServiceBus.AcceptanceTesting.Customization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

public static class LoggingBuilderExtensions
{
    extension(ILoggingBuilder loggingBuilder)
    {
        /// <summary>
        /// Adds a logger provider that appends the log messages to the scenario context.
        /// </summary>
        /// <param name="scenarioContext">The scenario context to append the log messages to.</param>
        public ILoggingBuilder AddContextAppender(ScenarioContext scenarioContext)
        {
            loggingBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(new ContextAppenderLoggerProvider(scenarioContext)));
            return loggingBuilder;
        }
    }
}