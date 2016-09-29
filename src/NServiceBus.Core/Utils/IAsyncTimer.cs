namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    interface IAsyncTimer
    {
        void Start(Func<Task> callback, TimeSpan interval, Action<Exception> errorCallback);
        Task Stop();
    }
}