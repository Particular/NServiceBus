namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    static partial class EnumerableExtensions
    {
        public static void WriteViolators<T>(this TextWriter writer, IEnumerable<T> violators) =>
            writer.WriteLine($"Violators:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");
    }
}
