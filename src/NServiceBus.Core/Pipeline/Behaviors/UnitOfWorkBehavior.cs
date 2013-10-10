namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using ObjectBuilder;
    using Unicast;

    public class UnitOfWorkBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public IBuilder Builder { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var unitOfWorkRunner = new UnitOfWorkRunner {Builder = Builder};

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