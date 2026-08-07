#nullable enable

namespace NServiceBus;

using System;

static class Host
{
    public static string GetOutputDirectory() => AppDomain.CurrentDomain.BaseDirectory;
}