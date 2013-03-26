namespace NServiceBus.CircuitBreakers
{
    using System;

    public interface ICircuitBreaker
    {
        bool Success();
        void Failure(Exception exception);
    }
}