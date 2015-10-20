namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class MarshallTestContextOnHandlerContextBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        ScenarioContext testContext;

        public MarshallTestContextOnHandlerContextBehavior(ScenarioContext context)
        {
            this.testContext = context;
        }

        public override Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            context.Set(testContext);

            return next();
        }
    }
}