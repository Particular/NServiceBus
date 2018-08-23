namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System;

    public readonly struct DiagnosticResultLocation
    {
        public DiagnosticResultLocation(string path, int line, int character)
        {
            if (line < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
            }

            if (character < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(character), "character must be >= -1");
            }

            Path = path;
            Line = line;
            Character = character;
        }

        public string Path { get; }

        public int Line { get; }

        public int Character { get; }
    }
}
