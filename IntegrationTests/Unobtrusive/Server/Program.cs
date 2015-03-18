using NServiceBus;
using Server;

class Program
{
    public static void Main()
    {
        var busConfiguration = new BusConfiguration();

        busConfiguration.EnableInstallers();
        busConfiguration.UsePersistence<InMemoryPersistence>();
        busConfiguration.UseDataBus<FileShareDataBus>()
            .BasePath(@"..\..\..\DataBusShare\");
        busConfiguration.RijndaelEncryptionService();

        var bus = Bus.Create(busConfiguration);
        bus.Start();
        new CommandSender {Bus = bus}.Start();
    }
}

