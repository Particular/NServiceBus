namespace NServiceBus.Core.Tests.TrimmedEndpoint;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class PackageConsumerTests
{
    [Test]
    [CancelAfter(1_800_000)]
    public async Task Packaged_consumer_gets_interceptors_and_runtime_trim_signal_without_explicit_props(CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(Path.GetTempPath(), "nservicebus-package-tests", Guid.NewGuid().ToString("N"));
        var feed = Path.Combine(root, "feed");
        var consumer = Path.Combine(root, "consumer");
        var publishOutput = Path.Combine(root, "publish");
        try
        {
            // 1. Pack NServiceBus.Core into a local feed.
            var coreProject = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..",
                "NServiceBus.Core",
                "NServiceBus.Core.csproj"));

            var packResult = await RunProcess("dotnet", $"pack \"{coreProject}\" -c Release -o \"{feed}\" --nologo", cancellationToken);
            Assert.That(packResult.ExitCode, Is.Zero, $"Pack failed:{Environment.NewLine}{packResult.Output}");

            var nupkg = Directory.GetFiles(feed, "NServiceBus.*.nupkg").SingleOrDefault();
            Assert.That(nupkg, Is.Not.Null, "No NServiceBus package was produced by the pack.");
            var packageVersion = Path.GetFileNameWithoutExtension(nupkg)["NServiceBus.".Length..];

            // 2. Create a consumer that references the PACKAGE (no project reference, no explicit
            //    CompilerVisibleProperty / InterceptorsNamespaces): everything must flow from the packed
            //    buildTransitive NServiceBus.props.
            Directory.CreateDirectory(consumer);
            await File.WriteAllTextAsync(Path.Combine(consumer, "nuget.config"), $$"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="local" value="{{feed}}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                </configuration>
                """, cancellationToken);

            await File.WriteAllTextAsync(Path.Combine(consumer, "PackageConsumer.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <LangVersion>preview</LangVersion>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <PublishTrimmed>true</PublishTrimmed>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="NServiceBus" Version="{{packageVersion}}" />
                  </ItemGroup>
                </Project>
                """, cancellationToken);

            await File.WriteAllTextAsync(Path.Combine(consumer, "Program.cs"), """
                using System.Text.Json;
                using System.Text.Json.Serialization;
                using NServiceBus;

                var configuration = new EndpointConfiguration("PackageConsumer");
                configuration.AssemblyScanner().Disable = true;
                configuration.UseSerialization<SystemJsonSerializer>().Options(new JsonSerializerOptions
                {
                    TypeInfoResolver = ConsumerJsonContext.Default
                });
                configuration.UseTransport<LearningTransport>().StorageDirectory(Path.Combine(Path.GetTempPath(), "nservicebus-package-consumer"));
                configuration.AddMessageType<MyCommand>();
                configuration.AddHandler<MyHandler>();

                var endpoint = await Endpoint.Start(configuration);
                IMessageSession session = endpoint;
                await session.SendLocal<MyCommand>(new MyCommand { SomeValue = "hello" });

                for (var i = 0; i < 100 && !MyHandler.Invoked; i++)
                {
                    await Task.Delay(100);
                }

                await endpoint.Stop();

                if (!MyHandler.Invoked)
                {
                    Console.Error.WriteLine("Handler was not invoked.");
                    return 1;
                }

                Console.WriteLine("TRIM-VALIDATION-SUCCESS");
                return 0;

                [Handler]
                public class MyHandler : IHandleMessages<MyCommand>
                {
                    public static bool Invoked;

                    public Task Handle(MyCommand message, IMessageHandlerContext context)
                    {
                        Invoked = true;
                        return Task.CompletedTask;
                    }
                }

                public class MyCommand : ICommand
                {
                    public string SomeValue { get; set; } = string.Empty;
                }

                [JsonSerializable(typeof(MyCommand))]
                public partial class ConsumerJsonContext : JsonSerializerContext
                {
                }
                """, cancellationToken);

            // 3. Publish the consumer trimmed. Interceptor support and the runtime trim signal must come from the
            //    packed NServiceBus.props, not from explicit project settings.
            var consumerProject = Path.Combine(consumer, "PackageConsumer.csproj");
            var publishResult = await RunProcess("dotnet",
                $"publish \"{consumerProject}\" -c Release -p:TreatWarningsAsErrors=false -o \"{publishOutput}\" --nologo",
                cancellationToken);

            Assert.That(publishResult.ExitCode, Is.Zero, $"Consumer publish failed:{Environment.NewLine}{publishResult.Output}");

            // The AddMessageType/AddHandler calls must be intercepted; otherwise the RequiresUnreferencedCode
            // fallback surfaces as IL2026 at the consumer's own call sites.
            var consumerTrimWarnings = publishResult.Output.Split(Environment.NewLine)
                .Where(line => line.Contains("Program.cs") && line.Contains("IL2026"))
                .ToArray();
            Assert.That(consumerTrimWarnings, Is.Empty, "Packaged interceptor support did not suppress IL2026 for the consumer source.");

            // 4. Run the packaged consumer executable.
            var executable = Path.Combine(publishOutput, OperatingSystem.IsWindows() ? "PackageConsumer.exe" : "PackageConsumer");
            var runResult = await RunProcess(executable, "", cancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runResult.ExitCode, Is.Zero, $"Packaged consumer failed:{Environment.NewLine}{runResult.Output}");
                Assert.That(runResult.Output, Does.Contain("TRIM-VALIDATION-SUCCESS"));
            }
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
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
