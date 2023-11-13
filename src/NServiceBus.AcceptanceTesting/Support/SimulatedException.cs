namespace NServiceBus.AcceptanceTesting;

using System;

/// <summary>
/// A dummy exception to be used in acceptance tests for easier differentiation from real exceptions.
/// </summary>
public class SimulatedException : Exception
{
    public SimulatedException()
    {
    }

    public SimulatedException(string message) : base(message)
    {
    }

    public SimulatedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}