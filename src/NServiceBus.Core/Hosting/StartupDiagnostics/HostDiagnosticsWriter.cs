namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class HostDiagnosticsWriter
    {
        public HostDiagnosticsWriter(Func<string, Task> writeData)
        {
            this.writeData = writeData;
        }

        public Task Write(string data)
        {
            return writeData(data);
        }

        Func<string, Task> writeData;
    }
}