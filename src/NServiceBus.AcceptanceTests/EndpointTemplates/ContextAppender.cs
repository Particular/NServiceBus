namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using log4net.Appender;
    using log4net.Core;

    public class ContextAppender : AppenderSkeleton
    {
        public ContextAppender(ScenarioContext context, EndpointConfiguration endpointConfiguration)
        {
            this.context = context;
            this.endpointConfiguration = endpointConfiguration;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (!endpointConfiguration.AllowExceptions && loggingEvent.ExceptionObject != null)
            {
                lock (context)
                {
                    context.Exceptions += loggingEvent.ExceptionObject + "/n/r";
                }
            }

        }

        ScenarioContext context;
        readonly EndpointConfiguration endpointConfiguration;
    }
}