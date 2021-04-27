namespace Nevermore.IntegrationTests.Model
{
    public class Boat : Vehicle
    {
        public string PortOfRegistry { get; set; }
        public Boat(string name, string portOfRegistry) : base(name)
        {
            PortOfRegistry = portOfRegistry;
        }
    }
}