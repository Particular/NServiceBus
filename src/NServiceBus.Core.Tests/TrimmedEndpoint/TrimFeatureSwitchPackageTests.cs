#nullable enable

namespace NServiceBus.Core.Tests.TrimmedEndpoint;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public class TrimFeatureSwitchPackageTests
{
    string feed = null!;
    string packageVersion = null!;
    string packagesDir = null!;
    string repositoryRoot = null!;

    [OneTimeSetUp]
    public async Task PackPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), "nservicebus-feature-switch-tests", Guid.NewGuid().ToString("N"));
        feed = Path.Combine(root, "feed");
        packagesDir = Path.Combine(root, "packages");
        Directory.CreateDirectory(feed);

        repositoryRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
        var coreProject = Path.Combine(repositoryRoot, "src", "NServiceBus.Core", "NServiceBus.Core.csproj");

        var packResult = await RunProcess("dotnet", $"pack \"{coreProject}\" -c Release -o \"{feed}\" --nologo", CancellationToken.None);
        Assert.That(packResult.ExitCode, Is.Zero, $"Pack failed:{Environment.NewLine}{packResult.Output}");

        var nupkg = Directory.GetFiles(feed, "NServiceBus.*.nupkg").SingleOrDefault();
        Assert.That(nupkg, Is.Not.Null, "No NServiceBus package was produced by the pack.");
        packageVersion = Path.GetFileNameWithoutExtension(nupkg!)["NServiceBus.".Length..];
    }

    [Test]
    public void Packed_package_contains_targets_in_build_and_buildTransitive()
    {
        var nupkg = Directory.GetFiles(feed, "NServiceBus.*.nupkg").Single();

        using var archive = ZipFile.OpenRead(nupkg);
        var entries = archive.Entries.Select(e => e.FullName).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries, Does.Contain("build/net10.0/NServiceBus.targets"));
            Assert.That(entries, Does.Contain("buildTransitive/net10.0/NServiceBus.targets"));
            Assert.That(entries, Does.Contain("build/net10.0/NServiceBus.props"));
            Assert.That(entries, Does.Contain("buildTransitive/net10.0/NServiceBus.props"));
        }
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task Trimmed_executable_emits_switch(CancellationToken cancellationToken = default)
    {
        var value = await GetSwitchValue("<OutputType>Exe</OutputType><PublishTrimmed>true</PublishTrimmed>", cancellationToken);

        Assert.That(value, Is.EqualTo("true"));
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task Aot_executable_emits_switch(CancellationToken cancellationToken = default)
    {
        var value = await GetSwitchValue("<OutputType>Exe</OutputType><PublishAot>true</PublishAot>", cancellationToken);

        Assert.That(value, Is.EqualTo("true"));
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task Explicit_true_enables_switch_without_trimmed_publish(CancellationToken cancellationToken = default)
    {
        var value = await GetSwitchValue(
            "<OutputType>Exe</OutputType><NServiceBusEnableStrictRegisteredOnlyMessageMetadata>true</NServiceBusEnableStrictRegisteredOnlyMessageMetadata>",
            cancellationToken);

        Assert.That(value, Is.EqualTo("true"));
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task Explicit_false_override_wins_over_automatic_default(CancellationToken cancellationToken = default)
    {
        var value = await GetSwitchValue(
            "<OutputType>Exe</OutputType><PublishTrimmed>true</PublishTrimmed><NServiceBusEnableStrictRegisteredOnlyMessageMetadata>false</NServiceBusEnableStrictRegisteredOnlyMessageMetadata>",
            cancellationToken);

        Assert.That(value, Is.EqualTo("false"));
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task IsTrimmable_alone_does_not_emit_switch(CancellationToken cancellationToken = default)
    {
        var value = await GetSwitchValue("<OutputType>Exe</OutputType><IsTrimmable>true</IsTrimmable>", cancellationToken);

        Assert.That(value, Is.Null);
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task IsAotCompatible_alone_does_not_emit_switch(CancellationToken cancellationToken = default)
    {
        var value = await GetSwitchValue("<OutputType>Exe</OutputType><IsAotCompatible>true</IsAotCompatible>", cancellationToken);

        Assert.That(value, Is.Null);
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task Library_does_not_emit_switch_when_publish_trimmed_flows_as_global_property(CancellationToken cancellationToken = default)
    {
        // MSBuild global properties (dotnet publish -p:PublishTrimmed=true) flow to every project in the build
        // graph; the switch must stay entry-point-scoped and not leak into library assemblies.
        var value = await GetSwitchValue("", "-p:PublishTrimmed=true", cancellationToken);

        Assert.That(value, Is.Null);
    }

    async Task<string?> GetSwitchValue(string projectBody, CancellationToken cancellationToken) =>
        await GetSwitchValue(projectBody, null, cancellationToken);

    async Task<string?> GetSwitchValue(string projectBody, string? globalProperty, CancellationToken cancellationToken)
    {
        var root = Path.Combine(Path.GetTempPath(), "nservicebus-feature-switch-tests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            CopyRepositoryConfiguration(root);
            await File.WriteAllTextAsync(Path.Combine(root, "Consumer.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    {{projectBody}}
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="NServiceBus" Version="{{packageVersion}}" />
                  </ItemGroup>
                </Project>
                """, cancellationToken);

            var consumerProject = Path.Combine(root, "Consumer.csproj");
            // A dedicated packages folder keeps the restore isolated from any stale global-cache extraction of the
            // same package version, so the freshly packed NServiceBus.targets is always the one that is imported.
            var restoreResult = await RunProcess("dotnet", $"restore \"{consumerProject}\" --packages \"{packagesDir}\" --nologo", cancellationToken);
            Assert.That(restoreResult.ExitCode, Is.Zero, $"Restore failed:{Environment.NewLine}{restoreResult.Output}");

            var getItemResult = await RunProcess("dotnet",
                $"msbuild \"{consumerProject}\" -getItem:RuntimeHostConfigurationOption -nologo -v:q {globalProperty}",
                cancellationToken);
            Assert.That(getItemResult.ExitCode, Is.Zero, $"msbuild -getItem failed:{Environment.NewLine}{getItemResult.Output}");

            using var document = JsonDocument.Parse(getItemResult.Output);
            var items = document.RootElement.GetProperty("Items").GetProperty("RuntimeHostConfigurationOption");
            foreach (var item in items.EnumerateArray())
            {
                if (item.GetProperty("Identity").GetString() == AppContextSwitches.StrictRegisteredOnlyMessageMetadataSwitchName)
                {
                    return item.GetProperty("Value").GetString();
                }
            }

            return null;
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Failed to clean up test directory {root}: {ex.Message}");
            }
        }
    }

    void CopyRepositoryConfiguration(string destination)
    {
        File.Copy(Path.Combine(repositoryRoot, "global.json"), Path.Combine(destination, "global.json"));

        var nugetConfigPath = Path.Combine(destination, "nuget.config");
        File.Copy(Path.Combine(repositoryRoot, "nuget.config"), nugetConfigPath);

        var nugetConfig = XDocument.Load(nugetConfigPath);
        var packageSources = nugetConfig.Root!.Element("packageSources")!;
        var localSource = packageSources.Elements("add")
            .SingleOrDefault(element => (string?)element.Attribute("key") == "local packages");
        if (localSource is null)
        {
            packageSources.Add(new XElement("add",
                new XAttribute("key", "local packages"),
                new XAttribute("value", feed)));
        }
        else
        {
            localSource.SetAttributeValue("value", feed);
        }

        nugetConfig.Save(nugetConfigPath);
    }

    static async Task<ProcessResult> RunProcess(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)!;

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(process.ExitCode, (await outputTask) + Environment.NewLine + (await errorTask));
    }

    sealed record ProcessResult(int ExitCode, string Output);
}
