#nullable enable

namespace NServiceBus;

// This class provides access to environment variables and gets overriden in the acceptance tests
// It is currently deliberately internal because we have some conceptual duplication. For example there is the RuntimeEnvironment class
// which is used to retrieve the machine name and there is also the HostInformation class which retrieves information about the host process.
// We should probably unify these concepts at some point in the future.
class SystemEnvironment
{
    public virtual string? GetEnvironmentVariable(string variable) => System.Environment.GetEnvironmentVariable(variable);
}