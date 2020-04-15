using Newtonsoft.Json;

namespace Nevermore.IntegrationTests.Model
{
    public abstract class Brand
    {
        public string Id { get; private set; }
        public string Name { get; set; }

        public abstract string Type { get; }

        public string Description { get; set; }
    }
}