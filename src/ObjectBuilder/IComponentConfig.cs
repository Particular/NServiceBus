namespace ObjectBuilder
{
    public interface IComponentConfig
    {
        IComponentConfig ConfigureProperty(string name, object value);
    }
}
