namespace Nevermore.IntegrationTests.Model
{
    public abstract class Endpoint
    {
        public string Name { get; set; }

        public abstract string Type { get; }
        
        public bool IsEnabled { get; set; }
    }
}