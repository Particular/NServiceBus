namespace NServiceBus.Core.Tests.Logging;

using System;
using System.IO;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

[TestFixture]
public class RollingLoggerProviderTests
{
    static ServiceProvider CreateServiceProvider(params ILoggerProvider[] providers)
    {
        var services = new ServiceCollection();
        foreach (var provider in providers)
        {
            services.AddSingleton(provider);
        }
        return services.BuildServiceProvider();
    }

    static RollingLoggerProvider CreateProvider(IServiceProvider serviceProvider, string directory, int numberOfArchiveFilesToKeep = 10, long maxFileSize = 10L * 1024 * 1024)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RollingLoggerProviderOptions
        {
            Directory = directory,
            NumberOfArchiveFilesToKeep = numberOfArchiveFilesToKeep,
            MaxFileSizeInBytes = maxFileSize
        });
        return new RollingLoggerProvider(serviceProvider, options);
    }

    [Test]
    public void When_line_is_written_line_appears_in_file()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogInformation("Foo");

        var singleFile = tempPath.GetSingle();
        var contents = NonLockingFileReader.ReadAllTextWithoutLocking(singleFile);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contents, Does.Contain("INFO "));
            Assert.That(contents, Does.Contain("Foo"));
        }
    }

    [Test]
    public void When_multiple_lines_are_written_lines_appear_in_file()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogInformation("Foo");
        logger.LogWarning("Bar");

        var singleFile = tempPath.GetSingle();
        var contents = NonLockingFileReader.ReadAllTextWithoutLocking(singleFile);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contents, Does.Contain("INFO "));
            Assert.That(contents, Does.Contain("Foo"));
            Assert.That(contents, Does.Contain("WARN "));
            Assert.That(contents, Does.Contain("Bar"));
        }
    }

    [Test]
    public void When_exception_is_logged_exception_appears_in_file()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        var exception = new InvalidOperationException("Test exception");
        logger.LogError(exception, "Error occurred");

        var singleFile = tempPath.GetSingle();
        var contents = NonLockingFileReader.ReadAllTextWithoutLocking(singleFile);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contents, Does.Contain("ERROR"));
            Assert.That(contents, Does.Contain("Error occurred"));
            Assert.That(contents, Does.Contain("System.InvalidOperationException: Test exception"));
        }
    }

    [Test]
    public void When_exception_with_data_is_logged_data_appears_in_file()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        var exception = new InvalidOperationException("Test exception");
        exception.Data["Key1"] = "Value1";
        logger.LogError(exception, "Error occurred");

        var singleFile = tempPath.GetSingle();
        var contents = NonLockingFileReader.ReadAllTextWithoutLocking(singleFile);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contents, Does.Contain("Exception details:"));
            Assert.That(contents, Does.Contain("Key1"));
            Assert.That(contents, Does.Contain("Value1"));
        }
    }

    [Test]
    public void When_log_level_is_filtered_lower_levels_are_not_written()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogDebug("Debug message");
        logger.LogInformation("Info message");
        logger.LogWarning("Warn message");

        var singleFile = tempPath.GetSingle();
        var contents = NonLockingFileReader.ReadAllTextWithoutLocking(singleFile);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contents, Does.Contain("DEBUG"));
            Assert.That(contents, Does.Contain("INFO"));
            Assert.That(contents, Does.Contain("WARN"));
        }
    }

    [Test]
    public void When_critical_level_is_used_fatal_is_written()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogCritical("Critical message");

        var singleFile = tempPath.GetSingle();
        var contents = NonLockingFileReader.ReadAllTextWithoutLocking(singleFile);
        Assert.That(contents, Does.Contain("FATAL"));
    }

    [Test]
    public void When_multiple_loggers_write_they_use_same_file()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger1 = provider.CreateLogger("Category1");
        var logger2 = provider.CreateLogger("Category2");

        logger1.LogInformation("From logger1");
        logger2.LogInformation("From logger2");

        var files = tempPath.GetFiles();
        Assert.That(files, Has.Count.EqualTo(1));

        var contents = NonLockingFileReader.ReadAllTextWithoutLocking(files[0]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contents, Does.Contain("From logger1"));
            Assert.That(contents, Does.Contain("From logger2"));
        }
    }

    [Test]
    public void When_file_is_too_large_new_sequence_file_is_created()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var provider = CreateProvider(CreateServiceProvider(), tempPath.TempDirectory, maxFileSize: 50);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogInformation("Some long text that exceeds the limit");

        var files = tempPath.GetFiles();
        Assert.That(files, Has.Count.EqualTo(1));

        logger.LogInformation("Another message");
        files = tempPath.GetFiles();

        Assert.That(files.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void When_file_name_is_created_it_follows_nservicebus_convention()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        using var serviceProvider = CreateServiceProvider();
        using var provider = CreateProvider(serviceProvider, tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogInformation("Test");

        var singleFile = tempPath.GetSingle();
        var fileName = Path.GetFileName(singleFile);
        Assert.Multiple(() =>
        {
            Assert.That(fileName, Does.StartWith("nsb_log_"));
            Assert.That(fileName, Does.EndWith(".txt"));
            Assert.That(fileName, Does.Match(@"nsb_log_\d{4}-\d{2}-\d{2}_\d+\.txt"));
        });
    }

    [Test]
    public void When_external_provider_is_present_provider_returns_null_logger()
    {
        using var tempPath = new TempPath("RollingLoggerTests");
        var externalProvider = new CustomTestProvider();
        using var provider = CreateProvider(CreateServiceProvider(externalProvider), tempPath.TempDirectory);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogInformation("This should not be logged");

        var files = tempPath.GetFiles();
        Assert.That(files, Has.Count.EqualTo(0));
    }

    sealed class CustomTestProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public void Dispose() { }
    }
}