#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "6.0",
        TreatAsErrorFromVersion = "5.0")]
    public class TransportConfiguration
    {
        [ObsoleteEx(
            Message = "Use `configuration.UseTransport<T>().ConnectionString(connectionString)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public void ConnectionString(string connectionString)
        {
            throw new InvalidOperationException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.UseTransport<T>().ConnectionStringName(name)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public void ConnectionStringName(string name)
        {
            throw new InvalidOperationException();
        }

        [ObsoleteEx(
            Message = "Use` configuration.UseTransport<T>().ConnectionString(connectionString)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.0")]
        public void ConnectionString(Func<string> connectionString)
        {
            throw new InvalidOperationException();
        }
    }
}