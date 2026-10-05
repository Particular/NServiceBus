namespace NServiceBus.Core.Tests.API;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NServiceBus.Core.Tests.API.Infra;
using NServiceBus.Logging;
using NUnit.Framework;
using Particular.Approvals;

// The legacy logging API (NServiceBus.Logging.LogManager.GetLogger and the
// NServiceBus.Logging.ILog logger fields it produces) is still used throughout
// NServiceBus.Core. The goal is to migrate this code to the high performance,
// source-generated loggers provided by Microsoft.Extensions.Logging using the
// [LoggerMessage] source generator.
//
// This test captures the set of types that still hold an NServiceBus.Logging.ILog
// logger field so that any new usage is immediately visible. When a type is migrated
// to a source-generated logger its ILog field is removed and it drops off this list,
// so the list naturally shrinks over time. It is acceptable for the list to grow
// only when the type affects user-facing code or when DI (dependency injection) is
// not available to obtain an ILogger<T>.
//
// See https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator
// for more details about the Microsoft.Extensions.Logging source generator.
[TestFixture]
public class LogManagerUsage
{
    [Test]
    public void ApproveLogManagerUsage()
    {
        var b = new StringBuilder()
            .AppendLine("The following types hold an NServiceBus.Logging.ILog logger field obtained via LogManager.GetLogger.")
            .AppendLine("For new code where DI is available, use the high performance source generated loggers from")
            .AppendLine("Microsoft.Extensions.Logging instead (see https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator).")
            .AppendLine("Changes that make this list longer should only be approved when the type affects user-facing")
            .AppendLine("code or when DI is not available. The list should otherwise shrink over time.")
            .AppendLine("-----");

        foreach (var type in NServiceBusAssembly.Types.OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            if (HasILogField(type))
            {
                b.AppendLine(type.FullName);
            }
        }

        Console.WriteLine(b.ToString());
        Approver.Verify(b.ToString());
    }

    static bool HasILogField(Type type)
    {
        // The NServiceBus.Logging namespace contains adapter types that intentionally
        // implement the legacy API surface and are not candidates for migration.
        if (type.Namespace == "NServiceBus.Logging")
        {
            return false;
        }

        // Compiler-generated state machines and closures are not meaningful migration
        // units; the ILog usage they contain belongs to their containing type.
        if (Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false))
        {
            return false;
        }

        return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Any(field => field.FieldType == typeof(ILog));
    }
}
