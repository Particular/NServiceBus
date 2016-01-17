namespace NServiceBus
{
    using System.Threading.Tasks;

    static class TaskEx
    {
        //TODO: remove when we update to 4.6 and can use Task.CompletedTask
        public static readonly Task CompletedTask = Task.FromResult(0);

        public static readonly Task<bool> TrueTask = Task.FromResult(true);
        public static readonly Task<bool> FalseTask = Task.FromResult(false);

        // ReSharper disable once UnusedParameter.Global
        // Used to explicitly suppress the compiler warning about 
        // using the returned value from async operations
        public static void Ignore(this Task task)
        {
        }
    }
}