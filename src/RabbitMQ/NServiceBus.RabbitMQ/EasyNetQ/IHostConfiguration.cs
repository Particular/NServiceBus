namespace EasyNetQ
{
    public interface IHostConfiguration
    {
        string Host { get; }
        ushort Port { get; }
    }
}