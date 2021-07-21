namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    interface ICircuitBreaker
    {
        void Success();
        Task Failure(Exception exception);
    }
}