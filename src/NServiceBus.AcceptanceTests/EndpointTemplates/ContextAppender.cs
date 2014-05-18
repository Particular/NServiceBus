namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    extern alias realOne;

    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using AppenderSkeleton = realOne::log4net.Appender.AppenderSkeleton;
    using LoggingEvent = realOne::log4net.Core.LoggingEvent;

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