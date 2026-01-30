#nullable enable

namespace NServiceBus;

class SystemEnvironment
{
    public virtual string? GetEnvironmentVariable(string variable) => System.Environment.GetEnvironmentVariable(variable);
}