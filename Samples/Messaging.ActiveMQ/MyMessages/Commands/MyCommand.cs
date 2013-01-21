namespace MyMessages.Commands
{
    using NServiceBus;

    public class MyCommand:ICommand
    {
        public string Name { get; set; }
    }
}
