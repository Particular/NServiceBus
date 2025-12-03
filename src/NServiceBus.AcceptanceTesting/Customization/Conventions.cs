namespace NServiceBus.AcceptanceTesting.Customization;

using System;

public static class Conventions
{
    static Conventions() => EndpointNamingConvention = static t => t.Name;

    public static Func<Type, string> EndpointNamingConvention { get; set; }
}