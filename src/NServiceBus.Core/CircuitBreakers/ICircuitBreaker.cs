namespace NServiceBus.CircuitBreakers
{
    using System;

    [ObsoleteEx(
        Message = "Not a public API.",
        TreatAsErrorFromVersion = "4.3", 
        RemoveInVersion = "5.0")]
    public interface ICircuitBreaker
    {
        bool Success();
        void Failure(Exception exception);
    }
}