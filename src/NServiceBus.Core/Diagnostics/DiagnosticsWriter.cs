namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class DiagnosticsWriter
    {
        public DiagnosticsWriter(Func<string, Task> writeData)
        {
            this.writeData = writeData;
        }

        Func<string, Task> writeData;

        public Task Write(string data)
        {
            return writeData(data);
        }
    }
}