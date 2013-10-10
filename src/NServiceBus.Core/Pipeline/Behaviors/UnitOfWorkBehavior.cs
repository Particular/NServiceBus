namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using ObjectBuilder;
    using Unicast;

    public class UnitOfWorkBehavior : IBehavior
    {
        readonly IBuilder builder;
        public IBehavior Next { get; set; }

        public UnitOfWorkBehavior(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Invoke(IBehaviorContext context)
        {
            var unitOfWorkRunner = new UnitOfWorkRunner {Builder = builder};

            try
            {
                context.Trace("Starting uow");
                unitOfWorkRunner.Begin();

                Next.Invoke(context);

                context.Trace("Ending uow");
                unitOfWorkRunner.End();
            }
            catch (Exception exception)
            {
                context.Trace("Appending exception: {0}", exception);
                unitOfWorkRunner.AppendEndExceptionsAndRethrow(exception);
            }
        }
    }
}