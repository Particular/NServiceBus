#nullable enable

namespace NServiceBus.Core.Tests.API.Infra.NullableInterface
{
    interface ITestInterface
    {
        string WriteAMessage(string message);

        string WriteNullableMessage(string? message);

        string? ReturnNullableMessage(string? message);
    }
}
