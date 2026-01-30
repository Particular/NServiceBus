namespace NServiceBus.AcceptanceTesting.Support;

using Settings;

class SettingsSystemEnvironment(SettingsHolder settings) : SystemEnvironment
{
    public override string? GetEnvironmentVariable(string variable) => settings.GetOrDefault<string>($"ACCEPTANCETEST_ENV:{variable}");
}