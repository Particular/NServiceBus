namespace NServiceBus.Testing
{
    using System.IO;

    class TextWriterLogger
    {
        public TextWriterLogger(TextWriter textWriter)
        {
            writer = textWriter;
        }

        public void Write(string message)
        {
            writer.WriteLine(message);
        }

        TextWriter writer;
    }
}