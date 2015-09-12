namespace NServiceBus
{
    using System.Threading.Tasks;

    static class TaskEx
    {
        //TODO: remove when we update to 4.6 and can use Task.CompletedTask
        public static readonly Task Completed = Task.FromResult(0);

        // ReSharper disable once UnusedParameter.Global
        public static void Ignore(this Task task)
        {
            // Intentionally left blank
        }
    }
}