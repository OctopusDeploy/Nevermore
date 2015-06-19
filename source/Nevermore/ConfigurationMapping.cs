namespace Nevermore
{
    public class ConfigurationMapping<T> : DocumentMap<T>
    {
        public ConfigurationMapping()
        {
            TableName = "Configuration";
        }
    }
}