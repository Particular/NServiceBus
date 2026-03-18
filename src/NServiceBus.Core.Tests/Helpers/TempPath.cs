#nullable enable

namespace NServiceBus.Core.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class TempPath : IDisposable
{
    public TempPath(string category)
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), category, Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(TempDirectory);
    }

    public readonly string TempDirectory;

    public IReadOnlyList<string> GetFiles() => [.. Directory.GetFiles(TempDirectory).OrderBy(x => x)];

    public string GetSingle() => GetFiles().Single();

    public void Dispose() => Directory.Delete(TempDirectory, true);
}